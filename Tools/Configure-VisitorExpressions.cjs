const fs=require('fs'),crypto=require('crypto');const root='Assets/ReTuneScenes/Art/Expressions/';
const groups=[
 {name:'Postman',folder:'邮差',crop:[350,91,619,948],files:['邮差-惊恐2.png','邮差-回避眼神3.png','邮差-腼腆微笑4.png']},
 {name:'Guitarist',folder:'吉他手',crop:[193,129,664,811],files:['吉他手-平静2.png','吉他手-气笑了3.png','吉他手-平静4.png']},
 {name:'BookshopOwner',folder:'书店老板',crop:[382,0,437,999],files:['书店老板-抬眼开口2.png','书店老板-侧视微笑3.png','书店老板-眯眼笑4.png']},
 {name:'Photographer',folder:'摄影师',crop:[258,129,660,886],files:['摄影师-回忆2.png','摄影师-释然的笑3.png','摄影师-眯眼笑4.png']}
];
function guid(p){return fs.readFileSync(p+'.meta','utf8').match(/^guid: (\w+)/m)[1];}
for(let group of groups)for(let [i,file] of group.files.entries()){
 let p=root+group.folder+'/'+file,meta=p+'.meta';let uid=fs.existsSync(meta)?guid(p):crypto.randomBytes(16).toString('hex');
 let s=fs.readFileSync('Assets/ReTuneScenes/Art/Portraits/Guitarist.png.meta','utf8').replace(/\r\n/g,'\n').replace(/^guid: \w+/m,'guid: '+uid).replace(/Guitarist_Seated/g,group.name+'_Expression'+(i+2)).replace(/spriteID: \w+/g,()=> 'spriteID: '+crypto.randomBytes(16).toString('hex')).replace(/maxTextureSize: \d+/g,'maxTextureSize: 4096').replace(/spriteMeshType: \d+/,'spriteMeshType: 1');
 for(let [j,key] of ['x','y','width','height'].entries())s=s.replace(new RegExp('        '+key+': \\d+'),'        '+key+': '+group.crop[j]);
 fs.writeFileSync(meta,s);
}
let runtime='Assets/ReTuneScenes/Runtime/ReTuneVisitorExpressions.cs';if(!fs.existsSync(runtime+'.meta'))fs.writeFileSync(runtime+'.meta','fileFormatVersion: 2\nguid: '+crypto.randomBytes(16).toString('hex')+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n');
let report=[];
for(let scene of ['01_Shop','04_Reply']){
 let path='Assets/ReTuneScenes/Scenes/'+scene+'.unity',s=fs.readFileSync(path,'utf8').replace(/\r\n/g,'\n'),bs=s.split(/(?=^--- !u!)/m),id=b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1],get=i=>bs.find(b=>id(b)===i);
 let controller=bs.find(b=>b.includes('  portraits:')),owner=controller.match(/m_GameObject: \{fileID: (\d+)/)[1];
 let portraits=controller.match(/  portraits:([\s\S]*?)  trialButton:/)[1].match(/\d+/g),comp=bs.find(b=>b.includes('guid: '+guid(runtime)));let cid=comp?id(comp):'995300000';
 if(!comp){let n=bs.findIndex(b=>id(b)===owner);bs[n]=bs[n].replace('  m_Layer:',`  - component: {fileID: ${cid}}\n  m_Layer:`);}
 let content=`--- !u!114 &${cid}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${owner}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${guid(runtime)}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  visitors:\n`;
 for(let [i,g] of portraits.entries()){
  let image=bs.find(b=>b.includes('m_GameObject: {fileID: '+g+'}')&&b.includes('guid: fe87c0e1cc204ed48ad3b37840f39efc'));
  if(!image)throw Error('Portrait image '+g);let initial=image.match(/m_Sprite: (.*)/)[1];
  content+=`  - portrait: {fileID: ${id(image)}}\n    expressions:\n    - ${initial}\n`;
  let group=groups[i];for(let file of group.files)content+=`    - {fileID: 21300000, guid: ${guid(root+group.folder+'/'+file)}, type: 3}\n`;
  report.push({scene,visitor:group.name,portrait:g,initialSprite:initial,crop:group.crop,expressions:4});
 }
 if(comp)bs[bs.indexOf(comp)]=content;else bs.push(content);fs.writeFileSync(path,bs.join(''));
}
for(let folder of [root.slice(0,-1),...groups.map(g=>root+g.folder)])if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta','fileFormatVersion: 2\nguid: '+crypto.randomBytes(16).toString('hex')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n');
fs.writeFileSync('Logs/VisitorExpressions-validation.json',JSON.stringify(report,null,2));console.log('Configured 4 expressions for 4 visitors in Shop and Reply; initial images and transforms unchanged.');
