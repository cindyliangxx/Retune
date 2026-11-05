const fs=require('fs'),crypto=require('crypto');const dir='Assets/ReTuneScenes/Runtime/';let p=dir+'ReTuneUITheme.cs.meta';if(!fs.existsSync(p))fs.writeFileSync(p,`fileFormatVersion: 2\nguid: ${crypto.randomBytes(16).toString('hex')}\n`);const themeGuid=fs.readFileSync(p,'utf8').match(/guid: (\w+)/)[1];const gradientGuid=fs.readFileSync(dir+'ReTuneSubtitleGradient.cs.meta','utf8').match(/guid: (\w+)/)[1];
let total=0,buttonCount=0;
for(let file of fs.readdirSync('Assets/ReTuneScenes/Scenes').filter(f=>f.endsWith('.unity'))){
 p='Assets/ReTuneScenes/Scenes/'+file;let s=fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n'),bs=s.split(/(?=^--- !u!)/m);let next=991000000;
 const id=b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1];const go=b=>b.match(/m_GameObject: \{fileID: (\d+)/)?.[1];
 const get=i=>bs.find(b=>id(b)===i);function change(i,fn){let n=bs.findIndex(b=>id(b)===i);if(n<0)throw Error(i);bs[n]=fn(bs[n]);}
 const comps=(g,guid)=>bs.filter(b=>go(b)===g&&(!guid||b.includes('guid: '+guid)));
 const trans=g=>comps(g).find(b=>/^--- !u!(4|224) /.test(b));
 const color=(b,c)=>b.replace(/  m_Color: \{[^\n]+\}/,'  m_Color: '+c);
 const ink='{r: 1, g: 0.96, b: 0.86, a: 1}',accent='{r: 1, g: 0.79, b: 0.43, a: 1}';
 function component(g,guid){let existing=comps(g,guid)[0];if(existing)return;while(get(String(next)))next++;let cid=String(next++);change(g,b=>b.replace('  m_Layer:',`  - component: {fileID: ${cid}}\n  m_Layer:`));bs.push(`--- !u!114 &${cid}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${g}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n`);}
 let ctrl=bs.find(b=>b.includes('overlayTitle:'));if(!ctrl)continue;
 component(go(ctrl),themeGuid);
 function descendants(g){let set=new Set([g]),again=true;while(again){again=false;for(let b of bs.filter(b=>/^--- !u!(4|224) /.test(b))){let parent=b.match(/m_Father: \{fileID: (\d+)/)?.[1];let parentGo=go(get(parent)||'');if(set.has(parentGo)&&!set.has(go(b))){set.add(go(b));again=true;}}}return set;}
 const buttons=bs.filter(b=>b.includes('guid: 4e29b1a8efbd4b44bb3f3716e73f07ff'));
 for(let button of buttons){let g=go(button),name=get(g).match(/m_Name: (.*)/)?.[1];if(name==='QTE_Note')continue;let nodes=descendants(g);let texts=bs.filter(b=>nodes.has(go(b))&&b.includes('guid: 5f7201a12d95ffc409449d95f23cf332'));if(!texts.length)continue;
  for(let image of comps(g,'fe87c0e1cc204ed48ad3b37840f39efc'))change(id(image),b=>color(b,'{r: 1, g: 1, b: 1, a: 0}'));
  for(let text of texts)change(id(text),b=>color(b,ink));
  change(id(button),b=>b.replace(/  m_TargetGraphic: \{[^\n]+\}/,`  m_TargetGraphic: {fileID: ${id(texts[0])}}`).replace('  m_Transition: 0','  m_Transition: 1').replace(/m_NormalColor: \{[^\n]+\}/,'m_NormalColor: {r: 1, g: 1, b: 1, a: 1}').replace(/m_HighlightedColor: \{[^\n]+\}/,'m_HighlightedColor: '+accent).replace(/m_SelectedColor: \{[^\n]+\}/,'m_SelectedColor: '+accent).replace(/m_PressedColor: \{[^\n]+\}/,'m_PressedColor: {r: 0.82, g: 0.64, b: 0.38, a: 1}').replace(/m_FadeDuration: [^\n]+/,'m_FadeDuration: 0.15'));
  buttonCount++;
 }
 const field=n=>ctrl.match(new RegExp('  '+n+': \\{fileID: (\\d+)'))?.[1];
 const overlay=field('overlay'),title=field('overlayTitle'),body=field('overlayBody');
 if(!overlay||!title||!body)continue;
 for(let image of comps(overlay,'fe87c0e1cc204ed48ad3b37840f39efc'))change(id(image),b=>color(b,'{r: 0.12, g: 0.09, b: 0.07, a: 0.42}'));
 const titleTr=trans(go(get(title))),panelTr=get(titleTr.match(/m_Father: \{fileID: (\d+)/)[1]),panelGo=go(panelTr);
 function rect(i,opts){change(i,b=>{for(let [n,v] of Object.entries(opts))b=b.replace(new RegExp('  '+n+': \\{[^\\n]+\\}'),'  '+n+': '+v);return b;});}
 rect(id(panelTr),{m_AnchorMin:'{x: 0.5, y: 0.5}',m_AnchorMax:'{x: 0.5, y: 0.5}',m_Pivot:'{x: 0.5, y: 0.5}',m_AnchoredPosition:'{x: 0, y: 0}',m_SizeDelta:'{x: 1160, y: 540}'});
 for(let image of comps(panelGo,'fe87c0e1cc204ed48ad3b37840f39efc')){change(id(image),b=>color(b,'{r: 0.17, g: 0.12, b: 0.09, a: 0.64}'));component(panelGo,gradientGuid);}
 for(let [text,top,height,size,tint] of [[title,48,64,42,accent],[body,136,310,36,ink]]){
  let t=trans(go(get(text)));rect(id(t),{m_AnchorMin:'{x: 0, y: 1}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 1}',m_AnchoredPosition:`{x: 0, y: -${top}}`,m_SizeDelta:`{x: -128, y: ${height}}`});
  change(text,b=>color(b,tint).replace(/m_FontSize: \d+/,'m_FontSize: '+size).replace(/m_Alignment: \d+/,'m_Alignment: 0').replace(/m_LineSpacing: [^\n]+/,'m_LineSpacing: 1.35').replace(/m_BestFit: \d+/,'m_BestFit: '+0).replace(/m_MinSize: \d+/,'m_MinSize: 22').replace(/m_MaxSize: \d+/,'m_MaxSize: '+size));
 }
 const panelChildren=descendants(panelGo);for(let button of buttons)if(panelChildren.has(go(button))){let t=trans(go(button));rect(id(t),{m_AnchorMin:'{x: 1, y: 0}',m_AnchorMax:'{x: 1, y: 0}',m_Pivot:'{x: 1, y: 0}',m_AnchoredPosition:'{x: -64, y: 34}',m_SizeDelta:'{x: 240, y: 56}'});}
 s=bs.join('');let ids=new Set(bs.map(id).filter(Boolean));for(let m of s.matchAll(/\{fileID: (\d+)\}/g))if(m[1]!=='0'&&!ids.has(m[1]))throw Error(file+' missing '+m[1]);fs.writeFileSync(p,s);total++;
}
console.log(`Unified ${total} scenes and ${buttonCount} text controls; QTE note visuals preserved.`);
