const fs=require('fs'),crypto=require('crypto');
const dir='Assets/ReTuneScenes/Art/Ending/';
function meta(p,template){if(!fs.existsSync(p+'.meta'))fs.writeFileSync(p+'.meta',template.replace(/^guid: \w+/m,'guid: '+crypto.randomBytes(16).toString('hex')));return fs.readFileSync(p+'.meta','utf8').match(/^guid: (\w+)/m)[1];}
const textureTemplate=fs.readFileSync('Assets/ReTuneScenes/Art/Signature/MichaelSignatureFinal.png.meta','utf8').replace(/\r\n/g,'\n').replace(/textureCompression: \d/g,'textureCompression: 0');
for(let file of ['Background1.png','Background2.png','LetterAnimation.png',...Array.from({length:14},(_,i)=>'Frames/Letter'+String(i+1).padStart(2,'0')+'.png')])meta(dir+file,textureTemplate);
for(let n of ['ReTuneEndingStoryboard','ReTuneProgress'])meta('Assets/ReTuneScenes/Runtime/'+n+'.cs','fileFormatVersion: 2\nguid: PLACEHOLDER\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n');
const guid=p=>fs.readFileSync(p+'.meta','utf8').match(/^guid: (\w+)/m)[1];
let path='Assets/ReTuneScenes/Scenes/05_Ending.unity',s=fs.readFileSync(path,'utf8').replace(/\r\n/g,'\n'),bs=s.split(/(?=^--- !u!)/m),nodes=[],next=995100000;
const id=b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1],get=i=>bs.find(b=>id(b)===String(i));function change(i,fn){let n=bs.findIndex(b=>id(b)===String(i));if(n<0)throw Error(i);bs[n]=fn(bs[n]);}
if(s.includes('Ending Artwork Layers')){console.log('Ending artwork is already configured; keeping scene edits.');process.exit(0);}
function node(name,parent,kind,opts={}){let g=next++,t=next++,renderer=next++,c=next++,n={g,t,c,parent,children:[]};nodes.push(n);if(typeof parent==='object'){parent.children.push(t);n.parent=parent.t;}n.blocks=()=>`--- !u!1 &${g}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: ${t}}\n  - component: {fileID: ${renderer}}\n  - component: {fileID: ${c}}\n  m_Layer: 5\n  m_Name: ${name}\n  m_TagString: Untagged\n  m_IsActive: ${opts.active===false?0:1}\n--- !u!224 &${t}\nRectTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${g}}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children:${n.children.length?'\n'+n.children.map(i=>'  - {fileID: '+i+'}').join('\n'):' []'}\n  m_Father: {fileID: ${n.parent}}\n  m_AnchorMin: {x: ${opts.stretch?0:.5}, y: ${opts.stretch?0:.5}}\n  m_AnchorMax: {x: ${opts.stretch?1:.5}, y: ${opts.stretch?1:.5}}\n  m_AnchoredPosition: {x: ${opts.x||0}, y: ${opts.y||0}}\n  m_SizeDelta: {x: ${opts.w??0}, y: ${opts.h??0}}\n  m_Pivot: {x: 0.5, y: 0.5}\n--- !u!222 &${renderer}\nCanvasRenderer:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${g}}\n  m_CullTransparentMesh: 1\n--- !u!114 &${c}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${g}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${kind==='raw'?'1344c3c82d62a2a41a3576d8abb8e3ea':kind==='text'?'5f7201a12d95ffc409449d95f23cf332':'fe87c0e1cc204ed48ad3b37840f39efc'}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  m_Material: {fileID: 0}\n  m_Color: ${opts.color||'{r: 1, g: 1, b: 1, a: 1}'}\n  m_RaycastTarget: 0\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n`+(kind==='raw'?`  m_Texture: ${opts.texture?'{fileID: 2800000, guid: '+guid(dir+opts.texture)+', type: 3}':'{fileID: 0}'}\n  m_UVRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1\n`:kind==='text'?`  m_FontData:\n    m_Font: {fileID: 12800000, guid: a628e6fb0cea48fcb4a46d22f443b58f, type: 3}\n    m_FontSize: ${opts.fontSize||44}\n    m_FontStyle: 0\n    m_BestFit: 0\n    m_MinSize: 16\n    m_MaxSize: 80\n    m_Alignment: ${opts.align??4}\n    m_RichText: 0\n    m_HorizontalOverflow: 0\n    m_VerticalOverflow: 0\n    m_LineSpacing: 1.2\n  m_Text: ${JSON.stringify(opts.text||'')}\n`:`  m_Sprite: {fileID: 0}\n  m_Type: 0\n  m_FillCenter: 1\n  m_FillAmount: 1\n`);return n;}
const black=node('Ending Letterbox',50145562,'image',{stretch:true,color:'{r: 0.08, g: 0.055, b: 0.035, a: 1}'});
const root=node('Ending Artwork Layers',50145562,'image',{w:1280,h:720,color:'{r: 1, g: 1, b: 1, a: 0}'});
node('01 Street and Postbox',root,'raw',{stretch:true,texture:'Background2.png'});
const figure=node('02 Michael - letter and departure',root,'raw',{stretch:true,texture:'LetterAnimation.png'});
node('03 Room and Doorframe Foreground',root,'raw',{stretch:true,texture:'Background1.png'});
const shade=node('04 Cinematic Fade',root,'image',{stretch:true,color:'{r: 0.08, g: 0.055, b: 0.035, a: 0}'});
const envelope=node('05 Sender Closeup',root,'image',{w:720,h:320,y:70,color:'{r: 0.82, g: 0.72, b: 0.53, a: 1}',active:false});
node('Envelope Paper',envelope,'image',{w:710,h:310,color:'{r: 0.94, g: 0.89, b: 0.76, a: 1}'});
node('Sender Label',envelope,'text',{w:240,h:45,x:-190,y:90,text:'From',fontSize:30,align:3,color:'{r: 0.36, g: 0.26, b: 0.19, a: 1}'});
node('Michael Signature',envelope,'text',{w:340,h:100,x:-125,y:20,text:'Michael',fontSize:76,align:3,color:'{r: 0.25, g: 0.19, b: 0.15, a: 1}'});
node('Recipient',envelope,'text',{w:400,h:65,x:65,y:-95,text:'To an old friend',fontSize:35,align:4,color:'{r: 0.36, g: 0.26, b: 0.19, a: 1}'});
const intertitle=node('06 Intertitle and Thanks',root,'text',{w:1100,h:480,text:'A few days later',fontSize:44,active:false,color:'{r: 1, g: 0.96, b: 0.86, a: 1}'});
change(50145562,b=>b.replace('  m_Children:\n',`  m_Children:\n  - {fileID: ${black.t}}\n  - {fileID: ${root.t}}\n`));
// The original world-space white boxes are replaced by the supplied full-frame artwork.
change(8000000001,b=>b.replace('  m_IsActive: 1','  m_IsActive: 0'));
let comp=next++;
change(131218311,b=>b.replace('  m_Layer:',`  - component: {fileID: ${comp}}\n  m_Layer:`));
bs.push(...nodes.map(n=>n.blocks()));
bs.push(`--- !u!114 &${comp}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 131218311}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${guid('Assets/ReTuneScenes/Runtime/ReTuneEndingStoryboard.cs')}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  player: {fileID: 131218314}\n  figure: {fileID: ${figure.c}}\n  restingFigure: {fileID: 2800000, guid: ${guid(dir+'LetterAnimation.png')}, type: 3}\n  letterFrames:\n${Array.from({length:14},(_,i)=>'  - {fileID: 2800000, guid: '+guid(dir+'Frames/Letter'+String(i+1).padStart(2,'0')+'.png')+', type: 3}').join('\n')}\n  envelope: {fileID: ${envelope.g}}\n  intertitle: {fileID: ${intertitle.c}}\n  shade: {fileID: ${shade.c}}\n  artwork: {fileID: ${root.t}}\n  framesPerSecond: 8\n  fadeDuration: 0.65\n`);
fs.writeFileSync(path,bs.join(''));
// Enable the previously unbound Tape hotspot and connect the saved-game action.
path='Assets/ReTuneScenes/Scenes/00_Start.unity';s=fs.readFileSync(path,'utf8').replace(/\r\n/g,'\n');bs=s.split(/(?=^--- !u!)/m);
const flow=bs.find(b=>b.includes('  mode: 1\n')&&b.includes('nextLabel:')),flowId=id(flow);
let button=bs.findIndex(b=>/^--- !u!114 &1221606816\n/.test(b));
if(button<0||!flowId)throw Error('Tape binding not found');
const eventTemplate=bs.find(b=>b.includes('m_MethodName: Next\n')&&b.includes('m_OnClick:'));
bs[button]=bs[button].slice(0,bs[button].indexOf('  m_OnClick:')).replace('  m_Interactable: 0','  m_Interactable: 1')+eventTemplate.slice(eventTemplate.indexOf('  m_OnClick:')).replace('m_MethodName: Next','m_MethodName: ResumeGame');
fs.writeFileSync(path,bs.join(''));
console.log('05_Ending: art layers + 14-frame sequence + closeup + intertitle. Tape: enabled and bound.');

{
const fs=require('fs');let p='Assets/ReTuneScenes/Scenes/05_Ending.unity',bs=fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n').split(/(?=^--- !u!)/m);
const id=b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1],get=i=>bs.find(b=>id(b)===String(i)),change=(i,fn)=>{let n=bs.findIndex(b=>id(b)===String(i));bs[n]=fn(bs[n]);};
let componentId=995200000;
change(131218311,b=>b.replace('  m_Layer:',`  - component: {fileID: ${componentId}}\n  m_Layer:`));
bs.push(`--- !u!114 &${componentId}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 131218311}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: 9945901f33dc68446bcef265aeed2c64, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  player: {fileID: 131218314}\n  panelHeight: 184\n  dialogueFontSize: 40\n  backgroundColor: {r: 0.22, g: 0.16, b: 0.1, a: 0.78}\n  charactersPerSecond: 32\n  punctuationPause: 0.16\n`);
// Reuse the established postman portrait for the delivery closeup, underneath the envelope.
const template=bs.find(b=>b.startsWith('--- !u!1 ')&&b.includes('  m_Name: Envelope Paper\n'));
let oldg=id(template),ids=[...template.matchAll(/component: \{fileID: (\d+)/g)].map(m=>m[1]),map=new Map([[oldg,'995200010'],...ids.map((v,i)=>[v,String(995200011+i)])]);
for(let orig of [template,...ids.map(i=>get(i))]){let b=orig.replace(/(?<=&|fileID: )\d+/g,v=>map.get(v)||v);
 if(orig===template)b=b.replace('m_Name: Envelope Paper','m_Name: Postman Delivery Closeup').replace('m_IsActive: 1','m_IsActive: 0');
 if(b.startsWith('--- !u!224 '))b=b.replace(/m_Father: \{[^\n]+\}/,'m_Father: {fileID: 995100005}').replace(/m_AnchoredPosition: \{[^\n]+\}/,'m_AnchoredPosition: {x: -420, y: -10}').replace(/m_SizeDelta: \{[^\n]+\}/,'m_SizeDelta: {x: 300, y: 460}');
 if(b.includes('m_Sprite:')){let guid=fs.readFileSync('Assets/ReTuneScenes/Art/Portraits/Postman_Portrait.asset.meta','utf8').match(/^guid: (\w+)/m)[1];b=b.replace(/m_Sprite: \{[^\n]+\}/,`m_Sprite: {fileID: 21300000, guid: ${guid}, type: 2}`).replace(/m_Color: \{[^\n]+\}/,'m_Color: {r: 1, g: 1, b: 1, a: 1}');}bs.push(b);}
// Insert after the dimming layer, before the closeup.
change(995100005,b=>b.replace('  - {fileID: 995100025}', '  - {fileID: 995200011}\n  - {fileID: 995100025}'));
let storyboard=bs.find(b=>b.includes('  letterFrames:'));change(id(storyboard),b=>b.replace('  artwork:', '  postman: {fileID: 995200010}\n  artwork:'));
fs.writeFileSync(p,bs.join(''));

}
{
const fs=require('fs'),crypto=require('crypto');const base='Assets/ReTuneScenes/Art/Ending/';
let mat=fs.readFileSync('Assets/ReTuneScenes/Art/Signature/PhotoSignatureReveal.mat','utf8').replace('m_Name: PhotoSignatureReveal','m_Name: EndingSignatureInk').replace('_Visible: 0','_Visible: 1').replace('_FullReveal: 0','_FullReveal: 1').replace('_Color: {r: 1, g: 1, b: 1, a: 1}','_Color: {r: 0.25, g: 0.19, b: 0.15, a: 1}');
fs.writeFileSync(base+'EndingSignatureInk.mat',mat);let mg=crypto.randomBytes(16).toString('hex');fs.writeFileSync(base+'EndingSignatureInk.mat.meta','fileFormatVersion: 2\nguid: '+mg+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n');
let p='Assets/ReTuneScenes/Scenes/05_Ending.unity',s=fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n'),bs=s.split(/(?=^--- !u!)/m);const id=b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1];let go=bs.find(b=>b.startsWith('--- !u!1 ')&&b.includes('m_Name: Michael Signature\n')),gid=id(go),component=bs.findIndex(b=>b.includes('m_GameObject: {fileID: '+gid+'}')&&b.includes('guid: 5f7201a12d95ffc409449d95f23cf332'));
let tg=fs.readFileSync('Assets/ReTuneScenes/Art/Signature/MichaelSignatureReference.png.meta','utf8').match(/^guid: (\w+)/m)[1];
bs[component]=bs[component].slice(0,bs[component].indexOf('  m_FontData:')).replace('guid: 5f7201a12d95ffc409449d95f23cf332','guid: 1344c3c82d62a2a41a3576d8abb8e3ea').replace('m_Material: {fileID: 0}',`m_Material: {fileID: 2100000, guid: ${mg}, type: 2}`).replace(/m_Color: \{[^\n]+\}/,'m_Color: {r: 1, g: 1, b: 1, a: 1}')+`  m_Texture: {fileID: 2800000, guid: ${tg}, type: 3}\n  m_UVRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1\n`;
let tr=bs.findIndex(b=>b.startsWith('--- !u!224 ')&&b.includes('m_GameObject: {fileID: '+gid+'}'));bs[tr]=bs[tr].replace(/m_SizeDelta: \{[^\n]+\}/,'m_SizeDelta: {x: 280, y: 143}');
fs.writeFileSync(p,bs.join(''));
for(const dir of ['Assets/ReTuneScenes/Art/Ending','Assets/ReTuneScenes/Art/Ending/Frames'])if(!fs.existsSync(dir+'.meta'))fs.writeFileSync(dir+'.meta','fileFormatVersion: 2\nguid: '+crypto.randomBytes(16).toString('hex')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n');

}
