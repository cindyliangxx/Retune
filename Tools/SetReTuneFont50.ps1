$fontGuid='a628e6fb0cea48fcb4a46d22f443b58f'
foreach($file in (Get-ChildItem Assets/ReTuneScenes/Scenes -Filter '*.unity')) {
 $scene=Get-Content $file.FullName -Raw
 $blocks=[regex]::Matches($scene,'(?ms)^--- !u!(\d+) &(\d+)\r?\n.*?(?=^--- !u!|\z)')
 $names=@{};$rects=@{};$buttons=@{}
 foreach($block in $blocks){$v=$block.Value;$id=$block.Groups[2].Value;if($block.Groups[1].Value -eq '1'){$names[$id]=[regex]::Match($v,'m_Name: (.+)').Groups[1].Value};if($block.Groups[1].Value -eq '224'){$go=[regex]::Match($v,'m_GameObject: \{fileID: (\d+)\}').Groups[1].Value;$rects[$go]=$id};if($v -match 'm_OnClick:'){$go=[regex]::Match($v,'m_GameObject: \{fileID: (\d+)\}').Groups[1].Value;$buttons[$go]=$true}}
 foreach($block in $blocks){$v=$block.Value;$new=$v;$go=[regex]::Match($v,'m_GameObject: \{fileID: (\d+)\}').Groups[1].Value;$name=$names[$go]
 if($v -match 'm_FontData:'){
  $new=[regex]::Replace($new,'m_Font: \{[^\r\n]+\}',"m_Font: {fileID: 12800000, guid: $fontGuid, type: 3}")
  $new=[regex]::Replace($new,'m_FontSize: \d+','m_FontSize: 50');$new=[regex]::Replace($new,'m_BestFit: \d+','m_BestFit: 0');$new=[regex]::Replace($new,'m_MinSize: \d+','m_MinSize: 50');$new=[regex]::Replace($new,'m_MaxSize: \d+','m_MaxSize: 50')
 }
 if($block.Groups[1].Value -eq '224'){
  $match=[regex]::Match($v,'m_SizeDelta: \{x: ([\d.\-]+), y: ([\d.\-]+)\}')
  if($match.Success){$w=[double]$match.Groups[1].Value;$h=[double]$match.Groups[2].Value
   $isText=($blocks | Where-Object {$_.Value -match "m_GameObject: \{fileID: $go\}" -and $_.Value -match 'm_FontData:'}).Count -gt 0
   if($isText){$h=[math]::Max($h,68)}
   if($buttons.ContainsKey($go)){$w=[math]::Max($w,220);$h=[math]::Max($h,80)}
   if($name -eq 'Label'){$parent=[regex]::Match($v,'m_Father: \{fileID: (\d+)\}').Groups[1].Value;$buttonGo=$buttons.Keys | Where-Object {$rects[$_] -eq $parent};if($buttonGo){$w=[math]::Max($w,204);$h=72}}
   if($name -eq 'Overlay Panel'){$w=1100;$h=580;$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 90, y: -70}')}
   if($name -eq 'Overlay Title'){$w=1020;$h=72}
   if($name -eq 'Overlay Text'){$w=1020;$h=330;$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 35, y: -110}')}
   if($name -eq 'Continue'){$w=280;$h=80;$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 750, y: -490}')}
   if($name -eq 'Start Panel'){$w=1120;$h=480;$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 80, y: -140}')}
   if($name -eq 'Intro Title'){$w=1040;$h=80}
   if($name -eq 'Intro Text'){$w=1040;$h=220;$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 30, y: -130}')}
   if($name -eq 'Start'){$w=280;$h=80;$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 30, y: -380}')}
   if($name -eq 'Retry Button'){$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 780, y: -630}')}
   if($name -eq 'Main Menu Button'){$new=[regex]::Replace($new,'m_AnchoredPosition: .*','m_AnchoredPosition: {x: 1020, y: -630}')}
   $new=[regex]::Replace($new,'m_SizeDelta: .*',"m_SizeDelta: {x: $w, y: $h}")
  }
 }
 if($new -ne $v){$scene=$scene.Replace($v,$new)}
 }
 Set-Content -LiteralPath $file.FullName -Encoding utf8 -Value $scene
}
