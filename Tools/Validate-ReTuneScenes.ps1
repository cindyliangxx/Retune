param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($ProjectRoot)
function Check([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function GuidOf([string]$Path) { return [regex]::Match((Get-Content -LiteralPath ($Path + '.meta') -Raw), 'guid: ([a-f0-9]{32})').Groups[1].Value }
$assetRoot = Join-Path $root 'Assets/ReTuneScenes'
$controllerSource = Get-Content -LiteralPath "$assetRoot/Runtime/ReTuneSceneController.cs" -Raw
$controllerGuid = GuidOf "$assetRoot/Runtime/ReTuneSceneController.cs"
$dragGuid = GuidOf "$assetRoot/Runtime/ReTunePartDrag.cs"
$storyGuid = GuidOf "$assetRoot/Data/Story.asset"
$build = Get-Content -LiteralPath (Join-Path $root 'ProjectSettings/EditorBuildSettings.asset') -Raw
$names = @('00_Start', '01_Shop', '02_Parts', '03_Performance', '04_Reply', '05_Ending')
$sceneEntries = [regex]::Matches($build, '(?m)^    path: (.+)$')
Check ($sceneEntries[0].Groups[1].Value.Trim() -eq 'Assets/ReTuneScenes/Scenes/00_Start.unity') 'Start scene must be first in build list'
$guidPaths = @{}
foreach ($searchRoot in @((Join-Path $root 'Assets'), (Join-Path $root 'Library/PackageCache'))) {
    if (!(Test-Path -LiteralPath $searchRoot)) { continue }
    foreach ($meta in Get-ChildItem -LiteralPath $searchRoot -Filter '*.meta' -Recurse -File) {
        $match = [regex]::Match((Get-Content -LiteralPath $meta.FullName -Raw), '(?m)^guid: ([a-f0-9]{32})')
        if ($match.Success) { $guidPaths[$match.Groups[1].Value] = $meta.FullName }
    }
}
for ($index = 0; $index -lt $names.Length; $index++) {
    $path = "$assetRoot/Scenes/$($names[$index]).unity"
    Check (Test-Path -LiteralPath $path) "Missing scene: $path"
    $scene = Get-Content -LiteralPath $path -Raw
    $sceneGuid = GuidOf $path
    $relative = "Assets/ReTuneScenes/Scenes/$($names[$index]).unity"
    Check ($build -match ('(?s)enabled: 1\s+path: ' + [regex]::Escape($relative) + '\s+guid: ' + $sceneGuid)) "Missing/incorrect build entry: $relative"
    $ids = @{}
    foreach ($match in [regex]::Matches($scene, '(?m)^--- !u!\d+ &(-?\d+)')) {
        $id = $match.Groups[1].Value
        Check (!$ids.ContainsKey($id)) "Duplicate fileID $id in $relative"
        $ids[$id] = $true
    }
    foreach ($match in [regex]::Matches($scene, '\{fileID: (-?\d+)\}')) {
        $id = $match.Groups[1].Value
        Check ($id -eq '0' -or $ids.ContainsKey($id)) "Broken local reference $id in $relative"
    }
    foreach ($match in [regex]::Matches($scene, 'guid: ([a-f0-9]{32})')) {
        $id = $match.Groups[1].Value
        Check ($id.StartsWith('0000000000000000') -or $guidPaths.ContainsKey($id)) "Unresolved asset GUID $id in $relative"
    }
    $blocks = [regex]::Split($scene, '(?m)(?=^--- !u!)')
    $blockById = @{}
    foreach ($block in $blocks) {
        $id = [regex]::Match($block, '^--- !u!\d+ &(\d+)').Groups[1].Value
        if ($id) { $blockById[$id] = $block }
    }
    $world = @($blocks | Where-Object { $_.StartsWith('--- !u!1 ') -and $_ -match '(?m)^  m_Name: "?World"?\r?$' })
    Check ($world.Count -eq 1) "Expected one authored World root: $relative"
    $worldTransform = [regex]::Match($world[0], 'component: \{fileID: (\d+)\}').Groups[1].Value
    Check ($blockById[$worldTransform].StartsWith('--- !u!4 ')) "World must use a plain Transform: $relative"
    Check ($scene -match '(?m)^  m_Name: "?UI_HUD"?\r?$') "Missing separate HUD: $relative"
    $spriteCount = [regex]::Matches($scene, '(?m)^SpriteRenderer:').Count
    Check ($spriteCount -gt 0) "Missing authored world sprites: $relative"
    Check ($scene.Contains('guid: 56666c5a40171f54783dd416a44f42bf')) "Missing camera Physics2DRaycaster: $relative"
    foreach ($block in $blocks) {
        if ($block.StartsWith('--- !u!1 ')) {
            $go = [regex]::Match($block, '^--- !u!1 &(\d+)').Groups[1].Value
            foreach ($component in [regex]::Matches($block, 'component: \{fileID: (\d+)\}')) {
                Check ($blockById[$component.Groups[1].Value] -match ("m_GameObject: \{fileID: $go\}")) "Component owner mismatch: $relative"
            }
        }
        if ($block.StartsWith('--- !u!4 ') -or $block.StartsWith('--- !u!224 ')) {
            $parentId = [regex]::Match($block, 'm_Father: \{fileID: (\d+)\}').Groups[1].Value
            $transformId = [regex]::Match($block, '^--- !u!\d+ &(\d+)').Groups[1].Value
            if ($parentId -ne '0') {
                $children = [regex]::Match($blockById[$parentId], '(?ms)  m_Children:.*?(?=  m_Father:)').Value
                Check ($children.Contains("{fileID: $transformId}")) "Parent does not list child: $relative / $transformId"
            }
        }
        if ($block.StartsWith('--- !u!61 ')) {
            $go = [regex]::Match($block, 'm_GameObject: \{fileID: (\d+)\}').Groups[1].Value
            $components = @([regex]::Matches($blockById[$go], 'component: \{fileID: (\d+)\}') | ForEach-Object { $blockById[$_.Groups[1].Value] })
            Check (@($components | Where-Object { $_.StartsWith('--- !u!212 ') }).Count -eq 1) "World collider needs a co-located SpriteRenderer for pointer sorting: $relative"
        }
    }
    $controllers = @($blocks | Where-Object { $_ -match ('m_Script: \{fileID: 11500000, guid: ' + $controllerGuid) })
    Check ($controllers.Count -eq 1) "Expected one saved controller in $relative"
    $controller = $controllers[0]
    $controllerId = [regex]::Match($controller, '^--- !u!114 &(\d+)').Groups[1].Value
    Check ($controller -match ('(?m)^  screen: ' + $index + '$')) "Wrong entry screen for $relative"
    Check ($controller.Contains("guid: $storyGuid")) "Wrong story asset for $relative"
    $required = @('worldCamera', 'customerLabel', 'hint', 'overlay', 'overlayTitle', 'overlayBody', 'trackA', 'trackB', 'trackC')
    if ($index -in @(1,4,5)) { $required += 'body' }
    if ($index -in @(1,4)) { $required += 'actionLabel' }
    if ($index -eq 2) { $required += @('structureSlot', 'toneSlot', 'structureLabel', 'toneLabel', 'trialButton', 'originalButton') }
    if ($index -eq 3) { $required += @('needle', 'approachRing', 'stats', 'noteLabel', 'stabilityFill', 'responseFill', 'holdFill', 'phraseFill') }
    foreach ($field in $required) { Check ($controller -match ('(?m)^  ' + $field + ': \{fileID: [1-9]\d*\}')) "Unassigned $field in $relative" }
    $textCount = [regex]::Matches($scene, '(?m)^  m_Text:').Count
    Check ($textCount -gt 5) "Scene lacks authored Text objects: $relative"
    $buttonCount = 0
    foreach ($block in $blocks) {
        foreach ($call in [regex]::Matches($block, '(?s)m_Target: \{fileID: (\d+)\}.*?m_MethodName: (\w+)')) {
            $method = $call.Groups[2].Value
            Check ($call.Groups[1].Value -eq $controllerId) "Button targets wrong controller: $relative"
            Check ($controllerSource -match ('public void ' + [regex]::Escape($method) + '\(')) "Button method missing: $method"
            $buttonCount++
        }
    }
    if ($index -eq 2) {
        Check ([regex]::Matches($scene, "guid: $dragGuid").Count -eq 6) 'Expected six authored draggable parts'
        foreach ($field in @('structureSlot', 'toneSlot')) {
            $id = [regex]::Match($controller, "(?m)^  ${field}: \{fileID: (\d+)\}").Groups[1].Value
            Check ($blockById[$id].StartsWith('--- !u!61 ')) "Slot is not a world collider: $field"
        }
        Check ([regex]::Matches($scene, '(?m)^BoxCollider2D:').Count -eq 8) 'Expected six part colliders and two slot colliders'
        foreach ($part in @($blocks | Where-Object { $_ -match "guid: $dragGuid" })) {
            $go = [regex]::Match($part, 'm_GameObject: \{fileID: (\d+)\}').Groups[1].Value
            $transform = [regex]::Match($blockById[$go], 'component: \{fileID: (\d+)\}').Groups[1].Value
            Check ($blockById[$transform].StartsWith('--- !u!4 ')) 'Draggable part still uses a UI RectTransform'
            Check ($blockById[$transform].Contains("m_Father: {fileID: $worldTransform}")) 'Part is not under World'
        }
    }
    if ($index -eq 3) { Check ([regex]::Matches($scene, '(?m)^BoxCollider2D:').Count -eq 1) 'Missing world mouse hit area' }
    Write-Output ("PASS {0}: {1} world sprites, {2} texts, {3} saved UI actions; hierarchy/refs/build entry valid" -f $names[$index], $spriteCount, $textCount, $buttonCount)
}
foreach ($source in Get-ChildItem -LiteralPath "$assetRoot/Runtime" -Filter '*.cs') {
    Check (!((Get-Content -LiteralPath $source.FullName -Raw) -match '\b(Instantiate|OnGUI)\s*\(|new\s+GameObject\s*\(')) "Runtime creates UI in $($source.Name)"
}
Check (Test-Path -LiteralPath (Join-Path $root 'Assets/Scenes/SampleScene.unity')) 'Original SampleScene is missing'
Check (Test-Path -LiteralPath (Join-Path $root 'Assets/ReTuneWhitebox/ReTuneWhitebox.unity')) 'Original whitebox is missing'
foreach ($folder in @('Assets/ReTuneScenes', 'Assets/ReTuneWhitebox')) {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File | Where-Object { $_.Extension -in '.cs', '.unity', '.asset' }) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $decoded = [regex]::Replace($content, '\\u([0-9a-fA-F]{4})', [Text.RegularExpressions.MatchEvaluator]{ param($match) [string][char][Convert]::ToInt32($match.Groups[1].Value, 16) })
        Check ($decoded -notmatch '\p{IsCJKUnifiedIdeographs}') "Chinese game text remains in $($file.FullName)"
    }
}
Write-Output 'ENGLISH_TEXT_SCAN_PASS (including escaped scene and story text).'
Write-Output 'STATIC_VALIDATION_PASS. No Unity editor launched. Does not verify pointer delivery, real-time timing, visuals, or audio.'
