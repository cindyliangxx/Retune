const fs=require('fs'),crypto=require('crypto');
const path='Assets/ReTuneScenes/Scenes/02_Parts.unity';
const read=p=>fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n');
const write=(p,s)=>fs.writeFileSync(p,s);
const guid=()=>crypto.randomBytes(16).toString('hex');
const metaGuid=p=>read(p+'.meta').match(/^guid: (\w+)/m)[1];
function assetMeta(p,type){if(!fs.existsSync(p+'.meta'))write(p+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\n${type}:\n  externalObjects: {}\n  mainObjectFileID: ${p.endsWith('.controller')?9100000:type==='NativeFormatImporter'?7400000:0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);return metaGuid(p);}
if(read(path).includes('originalRoller: {fileID: 828206476}')){console.log('Animated parts already configured; use the Unity menu to rebuild.');process.exit(0);}
const folder='Assets/美术资产/retune零件/滚轮动画/';
const output='Assets/ReTuneScenes/Animation/';
let template=read(output+'roller_Animation.anim');
let sourceRefs=[...template.matchAll(/value: (\{fileID: [^\n]+\})/g)].map(m=>m[1]);
const sourceGuids=[...new Set(sourceRefs)];
function build(name,files){
 let refs=files.map((file,i)=>{
  const p=folder+file+'.png';let m=read(p+'.meta');
  const templateMeta=read(folder+'滚轮1_1.png.meta');
  let crop=templateMeta.slice(templateMeta.indexOf('  spriteSheet:')).replace(/spriteID: [a-f0-9]{32}/g,()=>`spriteID: ${guid()}`).replace(/1375515245/g,'21300000').replace(/"\\u6EDA\\u8F6E1_1_0"/g,name+'_'+i);
  m=m.slice(0,m.indexOf('  spriteSheet:'))+crop;
  m=m.replace(/  spriteMode: \d+/, '  spriteMode: 2');write(p+'.meta',m);
  return `{fileID: 21300000, guid: ${metaGuid(p)}, type: 3}`;
 });
 let clip=template.replace('m_Name: roller_Animation','m_Name: '+name);
 for(let i=0;i<sourceGuids.length;i++)clip=clip.split(sourceGuids[i]).join(refs[i]);
 const clipPath=output+name+'.anim';write(clipPath,clip);const cg=assetMeta(clipPath,'NativeFormatImporter');
 const controllerPath=output+name+'.controller';let con=read(output+'roller.controller').replace('m_Name: roller\n','m_Name: '+name+'\n').replace(/guid: 5334530b8650a7441a0ce8794ffed6e6/g,'guid: '+cg);
 write(controllerPath,con);const ag=assetMeta(controllerPath,'NativeFormatImporter');
 return {controller:ag,first:refs[0]};
}
const soft=build('SoftRoller',['2026-11-01 221238','2026-11-01 221242','2026-11-01 221246']);
const warm=build('WarmRoller',['2026-11-01 221333','2026-11-01 221338','2026-11-01 221342']);
let s=read(path);let blocks=s.split(/(?=^--- !u!)/m);const get=id=>blocks.find(b=>new RegExp('^--- !u!\\d+ &'+id+'\\n').test(b));
function change(id,fn){let i=blocks.indexOf(get(id));if(i<0)throw Error('Missing '+id);blocks[i]=fn(blocks[i]);}
const goOf=b=>b.match(/m_GameObject: \{fileID: (\d+)/)[1];
const transformOf=go=>blocks.find(b=>/^--- !u!(4|224) /.test(b)&&goOf(b)===go);
const idOf=b=>b.match(/^--- !u!\d+ &(\d+)/)[1];
function addComponent(go,id){change(go,b=>b.replace('  m_Layer:',`  - component: {fileID: ${id}}\n  m_Layer:`));}
let nextId=990010000;function append(b){blocks.push(b);}
function mono(go,id,script,fields){return `--- !u!114 &${id}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${go}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${script}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n${fields}`;}
let dragBlocks=blocks.filter(b=>b.includes('guid: 55503b70b3b149c3a1e86c5fe9d9dcdb'));
const colliderTemplate=get('474920661');
for(let drag of dragBlocks){
 const part=Number(drag.match(/  part: (\d+)/)[1]),rootGo=goOf(drag),rootTransform=transformOf(rootGo),rootId=idOf(rootTransform);
 let descendants=new Set([rootId]);let changed=true;
 while(changed){changed=false;for(let b of blocks)if(/^--- !u!(4|224) /.test(b)&&descendants.has(b.match(/m_Father: \{fileID: (\d+)/)?.[1])&&!descendants.has(idOf(b))){descendants.add(idOf(b));changed=true;}}
 const gos=new Set([...descendants].map(id=>goOf(get(id))));
 const visuals=blocks.filter(b=>b.startsWith('--- !u!212 ')&&gos.has(goOf(b))&&b.includes('  m_Enabled: 1'));
 const visual=visuals.at(-1),visualGo=goOf(visual);
 for(let collider of blocks.filter(b=>/^--- !u!(61|58) /.test(b)&&gos.has(goOf(b)))){
   let cid=idOf(collider),owner=goOf(collider);change(owner,b=>b.replace(`  - component: {fileID: ${cid}}\n`,''));blocks.splice(blocks.indexOf(collider),1);
 }
 const cid=nextId++;addComponent(visualGo,cid);
 // Art is cropped to the sprite bounds; one collider follows each actual visual.
 let size=part>=4?'{x: 4.95, y: 2.52}':visual.match(/  m_Size: (\{[^\n]+\})/)[1];
 append(colliderTemplate.replace('&474920661','&'+cid).replace('m_GameObject: {fileID: 474920660}',`m_GameObject: {fileID: ${visualGo}}`).replace(/  m_Size: \{[^\n]+\}/,'  m_Size: '+size));
 if(part<4)continue;
 const data=part===4?soft:warm,aid=nextId++,mid=nextId++;
 addComponent(visualGo,aid);addComponent(visualGo,mid);
 const animator=get('828206473').replace('&828206473','&'+aid).replace('m_GameObject: {fileID: 828206472}',`m_GameObject: {fileID: ${visualGo}}`).replace(/guid: e25ae56c671551a44b31588011b0ae4a/,'guid: '+data.controller);
 append(animator);
 append(mono(visualGo,mid,'7f2ab68a046e9e2458204e3abda7543b',`  rollerAnimator: {fileID: ${aid}}\n  tines: []\n  baseSpeed: 1\n`));
 change(idOf(visual),b=>b.replace(/  m_Sprite: \{[^\n]+\}/,'  m_Sprite: '+data.first));
}
change('953785428',b=>b.replace('  roller: {fileID: 58244057}','  roller: {fileID: 828206475}').replace('  playbackDuration: 5','  playbackDuration: 5\n  originalRoller: {fileID: 828206476}'));
// Retire the static cylinder underneath the existing animated original.
for(let b of blocks.filter(b=>b.startsWith('--- !u!212 ')&&goOf(b)==='58244055'))change(idOf(b),b=>b.replace('  m_Enabled: 1','  m_Enabled: 0'));
const controller=blocks.find(b=>b.startsWith('--- !u!114 &953785426\n'));
function alignSlot(field,pos,size){
 const cid=controller.match(new RegExp('  '+field+': \\{fileID: (\\d+)'))[1],col=get(cid),go=goOf(col),tr=transformOf(go);
 // Slot parents are under the World root at identity; place the actual collider child in world space.
 const trId=idOf(tr),parent=tr.match(/m_Father: \{fileID: (\d+)/)[1];
 function worldPosition(id){if(id==='0')return [0,0];let b=get(id),v=b.match(/m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)/);let p=worldPosition(b.match(/m_Father: \{fileID: (\d+)/)[1]);return [Number(v[1])+p[0],Number(v[2])+p[1]];}
 let parentPos=worldPosition(parent);
 change(trId,b=>b.replace(/  m_LocalPosition: \{[^\n]+\}/,`  m_LocalPosition: {x: ${pos[0]-parentPos[0]}, y: ${pos[1]-parentPos[1]}, z: 0}`));
 change(cid,b=>b.replace(/  m_Offset: \{[^\n]+\}/,'  m_Offset: {x: 0, y: 0}').replace(/  m_Size: \{[^\n]+\}/,`  m_Size: {x: ${size[0]}, y: ${size[1]}}`));
 return cid;
}
alignSlot('structureSlot',[-.8,-1.35],[1.8,1.8]);const tone=alignSlot('toneSlot',[-2.7706,.26],[4.95*.65185404,2.52*.651854]);
change('953785428',b=>b.replace('  rollerArea: {fileID: 474920661}',`  rollerArea: {fileID: ${tone}}`));
write(path,blocks.join('').replace('targetZone: {fileID: 8000000058}','targetZone: {fileID: 990010000}')); 
for(let p of ['Assets/ReTuneScenes/Editor/ReTunePartsUpgrade.cs','Assets/ReTuneScenes/Editor/ReTunePartsSmoke.cs'])if(!fs.existsSync(p+'.meta'))write(p+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\n`);
console.log('Saved A/B loop clips, controllers, six collider bindings and replacement slots to '+path);
