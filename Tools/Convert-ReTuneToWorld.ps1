param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
$root=Join-Path $ProjectRoot 'Assets/ReTuneScenes'
function GuidOf($path) { [regex]::Match((Get-Content -LiteralPath ($path+'.meta') -Raw),'guid: ([a-f0-9]{32})').Groups[1].Value }
$dragGuid=GuidOf "$root/Runtime/ReTunePartDrag.cs"
$noteGuid=GuidOf "$root/Runtime/ReTuneNoteInput.cs"
$controllerGuid=GuidOf "$root/Runtime/ReTuneSceneController.cs"
$clickGuid=GuidOf "$root/Runtime/ReTuneWorldClick.cs"
$framingGuid=GuidOf "$root/Runtime/ReTuneWorldCamera.cs"
$squareGuid=GuidOf "$root/UI/Square.png"
$materialGuid=GuidOf (Join-Path $ProjectRoot 'Library/PackageCache/com.unity.render-pipelines.universal@14.0.12/Runtime/Materials/Sprite-Unlit-Default.mat')
$worldNames=@('Background','Shop Background','Open Music Box','Gears and Spring','Rhythm Slot_Click to Remove','Tone Slot_Click to Remove','Balance Dial','Mouse Hit Area','A_Melody Track','B_Reply Track','Letter')
function NewId { $script:next++; $script:next.ToString() }
function Number($value) { ([double]$value).ToString('0.########',[Globalization.CultureInfo]::InvariantCulture) }
function FieldId($text,$field) { [regex]::Match($text,('(?m)^  '+$field+': \{fileID: (\d+)\}')).Groups[1].Value }
function Vec($text,$field) { $m=[regex]::Match($text,('(?m)^  '+$field+': \{x: (-?[\d.eE+]+), y: (-?[\d.eE+]+)')); if(!$m.Success) { throw "Missing vector $field" }; @([double]::Parse($m.Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture),[double]::Parse($m.Groups[2].Value,[Globalization.CultureInfo]::InvariantCulture)) }
function Components($go) { @([regex]::Matches($script:blocks[$go],'component: \{fileID: (\d+)\}') | ForEach-Object { $_.Groups[1].Value }) }
function SetComponents($go,$ids) { $value="  m_Component:`n"; foreach($id in $ids) { $value+="  - component: {fileID: $id}`n" }; $script:blocks[$go]=[regex]::Replace($script:blocks[$go],'(?ms)  m_Component:\r?\n.*?(?=  m_Layer:)',$value) }
function Children($transform) { $m=[regex]::Match($script:blocks[$transform],'(?ms)  m_Children:.*?(?=  m_Father:)'); @([regex]::Matches($m.Value,'\{fileID: (\d+)\}') | ForEach-Object { $_.Groups[1].Value }) }
function SetChildren($transform,$ids) { $value=if(@($ids).Count) { "  m_Children:`n"+(@($ids | ForEach-Object { "  - {fileID: $_}" }) -join "`n")+"`n" } else { "  m_Children: []`n" }; $script:blocks[$transform]=[regex]::Replace($script:blocks[$transform],'(?ms)  m_Children:.*?(?=  m_Father:)',$value) }
function Header($class,$type,$id,$go) { @"
--- !u!$class &$id
${type}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $go}
"@+"`n" }
function GameObject($id,$name,$componentIds) { $list=($componentIds | ForEach-Object { "  - component: {fileID: $_}" }) -join "`n"; @"
--- !u!1 &$id
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
$list
  m_Layer: 0
  m_Name: $name
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"@+"`n" }
function Transform($id,$go,$parent,$x,$y,$rotation='{x: 0, y: 0, z: 0, w: 1}') { (Header 4 Transform $id $go)+@"
  serializedVersion: 2
  m_LocalRotation: $rotation
  m_LocalPosition: {x: $(Number $x), y: $(Number $y), z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: $parent}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"@+"`n" }
function Mono($id,$go,$guid,$fields) { (Header 114 MonoBehaviour $id $go)+@"
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $guid, type: 3}
  m_Name:
  m_EditorClassIdentifier:
$fields
"@+"`n" }
function Canvas($id,$go,$order) { (Header 223 Canvas $id $go)+@"
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 2
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 0
  m_OverrideSorting: 1
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 0
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: $order
  m_TargetDisplay: 0
"@+"`n" }
function Collider($id,$go,$width,$height,$ox,$oy) { (Header 61 BoxCollider2D $id $go)+@"
  m_Enabled: 1
  m_Density: 1
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {x: $(Number $ox), y: $(Number $oy)}
  m_AutoTiling: 0
  serializedVersion: 2
  m_Size: {x: $(Number $width), y: $(Number $height)}
  m_EdgeRadius: 0
"@+"`n" }
function Sprite($id,$go,$sprite,$color,$width,$height,$order) { (Header 212 SpriteRenderer $id $go)+@"
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: $materialGuid, type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: $order
  m_Sprite: $sprite
  m_Color: $color
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 1
  m_Size: {x: $(Number $width), y: $(Number $height)}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
"@+"`n" }
function ConvertNode($id,$parent,$parentSize,$parentPivot) {
    $old=$script:blocks[$id]; $go=FieldId $old 'm_GameObject'; $name=$script:names[$go]; $components=@(Components $go); $children=@(Children $id)
    $size=Vec $old 'm_SizeDelta'; $anchor=Vec $old 'm_AnchorMin'; $anchorMax=Vec $old 'm_AnchorMax'; $pivot=Vec $old 'm_Pivot'; $anchored=Vec $old 'm_AnchoredPosition'
    $size=@(($size[0]+($anchorMax[0]-$anchor[0])*$parentSize[0]),($size[1]+($anchorMax[1]-$anchor[1])*$parentSize[1]))
    $position=@(((($anchor[0]-$parentPivot[0])*$parentSize[0]+$anchored[0])/100),((($anchor[1]-$parentPivot[1])*$parentSize[1]+$anchored[1])/100))
    $script:order+=10; $sorting=$script:order
    $script:blocks[$go]=[regex]::Replace($script:blocks[$go],'(?m)^  m_Layer: \d+','  m_Layer: 0')
    $text=@($components | Where-Object { $script:blocks[$_] -match '(?m)^  m_Text:' })
    if($text.Count) {
        $new=$old
        $new=[regex]::Replace($new,'(?m)^  m_LocalPosition: .*$',"  m_LocalPosition: {x: $(Number $position[0]), y: $(Number $position[1]), z: 0}")
        $new=[regex]::Replace($new,'(?m)^  m_LocalScale: .*$', '  m_LocalScale: {x: 0.01, y: 0.01, z: 0.01}')
        $new=[regex]::Replace($new,'(?m)^  m_AnchorMin: .*$', '  m_AnchorMin: {x: 0.5, y: 0.5}')
        $new=[regex]::Replace($new,'(?m)^  m_AnchorMax: .*$', '  m_AnchorMax: {x: 0.5, y: 0.5}')
        $new=[regex]::Replace($new,'(?m)^  m_AnchoredPosition: .*$',"  m_AnchoredPosition: {x: $(Number $position[0]), y: $(Number $position[1])}")
        $new=[regex]::Replace($new,'(?m)^  m_Father: .*$',"  m_Father: {fileID: $parent}")
        $script:blocks[$id]=$new
        $canvasId=NewId; $script:blocks.Add($canvasId,(Canvas $canvasId $go $sorting)); SetComponents $go ($components+@($canvasId))
        return
    }
    $image=@($components | Where-Object { $script:blocks[$_] -match '(?m)^  m_Sprite:' })
    $part=@($components | Where-Object { $script:blocks[$_] -match "guid: $dragGuid" })
    $note=@($components | Where-Object { $script:blocks[$_] -match "guid: $noteGuid" })
    $rotation=[regex]::Match($old,'(?m)^  m_LocalRotation: (.+)$').Groups[1].Value.Trim()
    $script:blocks[$id]=Transform $id $go $parent $position[0] $position[1] $rotation
    $keep=[Collections.Generic.List[string]]::new(); $keep.Add([string]$id)
    foreach($component in $components) {
        if($component -eq $id) { continue }
        $block=$script:blocks[$component]
        if($block -match '^--- !u!(222|225) ' -or $block -match '(?m)^  m_Sprite:|^  m_Navigation:') { $script:blocks.Remove($component) }
        else { $keep.Add([string]$component) }
    }
    $ox=(.5-$pivot[0])*$size[0]/100; $oy=(.5-$pivot[1])*$size[1]/100
    $visualTransform=$null
    if($image.Count) {
        # The old Image block was removed above, so read its cached original value.
        $imageBlock=$script:original[$image[0]]
        $sprite=[regex]::Match($imageBlock,'(?m)^  m_Sprite: (.+)$').Groups[1].Value.Trim(); if($sprite -eq '{fileID: 0}') { $sprite="{fileID: 21300000, guid: $squareGuid, type: 3}" }
        $color=[regex]::Match($imageBlock,'(?m)^  m_Color: (.+)$').Groups[1].Value.Trim()
        $visualGo=NewId; $visualTransform=NewId; $renderer=NewId
        $script:blocks.Add($visualGo,(GameObject $visualGo 'Visual' @($visualTransform,$renderer)))
        $script:blocks.Add($visualTransform,(Transform $visualTransform $visualGo $id $ox $oy))
        $script:blocks.Add($renderer,(Sprite $renderer $visualGo $sprite $color ($size[0]/100) ($size[1]/100) $sorting))
    }
    foreach($child in $children) { ConvertNode $child $id $size $pivot }
    $isSlot=$name -in @('Rhythm Slot_Click to Remove','Tone Slot_Click to Remove')
    $isBox=$name -eq 'Music Box' -and $script:screen -eq 1
    if($part.Count -or $note.Count -or $isSlot -or $isBox) {
        $colliderId=NewId; $script:blocks.Add($colliderId,(Collider $colliderId $visualGo ($size[0]/100) ($size[1]/100) 0 0)); SetComponents $visualGo (@(Components $visualGo)+@($colliderId))
        if($isSlot -or $isBox) {
            $action=if($isBox) { 0 } elseif($name.StartsWith('Rhythm')) { 1 } else { 2 }
            $clickId=NewId; $script:blocks.Add($clickId,(Mono $clickId $go $clickGuid "  controller: {fileID: $script:controllerId}`n  action: $action")); $keep.Add($clickId)
            if($isSlot) { $field=if($action -eq 1) { 'structureSlot' } else { 'toneSlot' }; $script:blocks[$script:controllerId]=[regex]::Replace($script:blocks[$script:controllerId],"(?m)^  ${field}: .*?$","  ${field}: {fileID: $colliderId}") }
        }
        if($part.Count) { $partIndex=[int][regex]::Match($script:blocks[$part[0]],'(?m)^  part: (\d+)').Groups[1].Value; $script:parts[$partIndex]=$part[0] }
    }
    SetComponents $go $keep.ToArray()
    if($visualTransform) { SetChildren $id ($children+@($visualTransform)) } else { SetChildren $id $children }
}
foreach($scene in Get-ChildItem "$root/Scenes" -Filter '*.unity') {
    $raw=Get-Content -LiteralPath $scene.FullName -Raw
    if($raw -match '(?m)^  m_Name: "?World"?\r?$') { Write-Output "Skipped existing world: $($scene.Name)"; continue }
    $script:next=8000000000L; $script:order=0; $script:parts=@{}
    $script:blocks=[Collections.Specialized.OrderedDictionary]::new(); $script:original=@{}; $script:names=@{}
    $chunks=[regex]::Split($raw,'(?m)(?=^--- !u!)'); $preamble=$chunks[0]
    foreach($chunk in $chunks | Select-Object -Skip 1) { $id=[regex]::Match($chunk,'^--- !u!\d+ &(\d+)').Groups[1].Value; $script:blocks.Add($id,$chunk); $script:original[$id]=$chunk; if($chunk.StartsWith('--- !u!1 ')) { $script:names[$id]=[regex]::Match($chunk,'(?m)^  m_Name: (.+)$').Groups[1].Value.Trim().Trim('"') } }
    $canvasGo=@($script:names.Keys | Where-Object { $script:names[$_] -eq 'Canvas_Editable UI' })[0]
    $canvasTransform=@(Components $canvasGo | Where-Object { $script:blocks[$_].StartsWith('--- !u!224 ') })[0]
    $scalerId=@(Components $canvasGo | Where-Object { $script:blocks[$_] -match '(?m)^  m_ReferenceResolution:' })[0]
    $script:controllerId=@($script:blocks.Keys | Where-Object { $script:blocks[$_] -match "guid: $controllerGuid" })[0]
    $script:screen=[int][regex]::Match($script:blocks[$script:controllerId],'(?m)^  screen: (\d+)').Groups[1].Value
    foreach($id in @($script:names.Keys)) { $script:blocks[$id]=[regex]::Replace($script:blocks[$id],'(?m)^  m_Layer: \d+','  m_Layer: 5') }
    $worldGo=NewId; $worldTransform=NewId
    $script:blocks.Add($worldGo,(GameObject $worldGo 'World' @($worldTransform))); $script:blocks.Add($worldTransform,(Transform $worldTransform $worldGo '0' 0 0))
    $selected=[Collections.Generic.List[string]]::new(); $remaining=[Collections.Generic.List[string]]::new()
    foreach($child in @(Children $canvasTransform)) {
        $go=FieldId $script:blocks[$child] 'm_GameObject'; $name=$script:names[$go]
        if($name -in $worldNames -or $name.StartsWith('Portrait_') -or $name.StartsWith('Part_')) { $selected.Add($child); ConvertNode $child $worldTransform @(1280,720) @(.5,.5) } else { $remaining.Add($child) }
    }
    SetChildren $canvasTransform $remaining.ToArray(); SetChildren $worldTransform $selected.ToArray()
    $script:blocks[$canvasGo]=$script:blocks[$canvasGo].Replace('"Canvas_Editable UI"','UI_HUD')
    $cameraId=@($script:blocks.Keys | Where-Object { $script:blocks[$_].StartsWith('--- !u!20 ') })[0]
    $cameraGo=FieldId $script:blocks[$cameraId] 'm_GameObject'
    $script:blocks[$cameraGo]=[regex]::Replace($script:blocks[$cameraGo],'(?m)^  m_Layer: \d+','  m_Layer: 0')
    $script:blocks[$cameraId]=[regex]::Replace($script:blocks[$cameraId],'(?m)^  orthographic size: .*?$','  orthographic size: 3.6')
    $raycaster=NewId; $framing=NewId
    $script:blocks.Add($raycaster,(Mono $raycaster $cameraGo '56666c5a40171f54783dd416a44f42bf' "  m_EventMask:`n    serializedVersion: 2`n    m_Bits: 1`n  m_MaxRayIntersections: 0"))
    $script:blocks.Add($framing,(Mono $framing $cameraGo $framingGuid "  hudScaler: {fileID: $scalerId}`n  pixelsPerUnit: 100"))
    SetComponents $cameraGo (@(Components $cameraGo)+@($raycaster,$framing))
    $script:blocks[$script:controllerId]+="  worldCamera: {fileID: $cameraId}`n"
    if($script:parts.Count) { $script:blocks[$script:controllerId]+="  partObjects:`n"; foreach($i in ($script:parts.Keys | Sort-Object)) { $script:blocks[$script:controllerId]+="  - {fileID: $($script:parts[$i])}`n" } } else { $script:blocks[$script:controllerId]+="  partObjects: []`n" }
    $sceneRoots=@($script:blocks.Keys | Where-Object { $script:blocks[$_].StartsWith('--- !u!1660057539 ') })[0]
    $script:blocks[$sceneRoots]=$script:blocks[$sceneRoots].TrimEnd()+"`n  - {fileID: $worldTransform}`n"
    $result=$preamble+(@($script:blocks.Values) -join '')
    [IO.File]::WriteAllText($scene.FullName,$result,[Text.UTF8Encoding]::new($false))
    Write-Output ("Converted {0}: {1} world groups, {2} draggable world parts" -f $scene.Name,$selected.Count,$script:parts.Count)
}
foreach($meta in Get-ChildItem "$root/UI" -Filter '*.png.meta') {
    $raw=Get-Content $meta.FullName -Raw; $raw=$raw.Replace('spriteMeshType: 1','spriteMeshType: 0'); [IO.File]::WriteAllText($meta.FullName,$raw,[Text.UTF8Encoding]::new($false))
}
