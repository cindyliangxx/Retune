const fs=require('fs'),path=require('path');
const root=process.cwd(), assetGuids=new Map(),errors=[],warnings=[],scenes=[],layers=[];
function walk(dir){for(let item of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,item.name);if(item.isDirectory())walk(p);else if(p.endsWith('.meta')){const m=fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m);if(m)assetGuids.set(m[1],p.slice(0,-5).replaceAll('\\','/'));}}}
walk('Assets');if(fs.existsSync('Library/PackageCache'))walk('Library/PackageCache');
const build=fs.readFileSync('ProjectSettings/EditorBuildSettings.asset','utf8');
function field(b,name){return b?.match(new RegExp('^  '+name+': (.*)$','m'))?.[1]?.trim();}
function ref(b,name){return field(b,name)?.match(/fileID: (-?\d+)/)?.[1];}
function decode(s){try{return s?.startsWith('"')?JSON.parse(s):s;}catch{return s;}}
for(const file of fs.readdirSync('Assets/ReTuneScenes/Scenes').filter(f=>f.endsWith('.unity'))){
 const p='Assets/ReTuneScenes/Scenes/'+file,text=fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n');const blocks=text.split(/(?=^--- !u!)/m).filter(b=>b.startsWith('--- !u!'));const map=new Map();
 for(const b of blocks){let id=b.match(/^--- !u!\d+ &(-?\d+)/)[1];if(map.has(id))errors.push(file+': duplicate '+id);map.set(id,b);}
 const id=b=>b.match(/^--- !u!\d+ &(-?\d+)/)?.[1];const owner=b=>ref(b,'m_GameObject');
 for(let m of text.matchAll(/\{fileID: (-?\d+)\}/g))if(m[1]!=='0'&&!map.has(m[1]))errors.push(file+': missing ref '+m[1]);
 for(let m of text.matchAll(/guid: ([a-f0-9]{32})/g))if(!m[1].startsWith('0000000000000000')&&!assetGuids.has(m[1]))errors.push(file+': missing asset '+m[1]);
 const gos=blocks.filter(b=>b.startsWith('--- !u!1 '));const transforms=new Map(blocks.filter(b=>/^--- !u!(4|224) /.test(b)).map(b=>[owner(b),b]));
 const goName=g=>decode(field(map.get(g),'m_Name'));
 function ancestry(g){let list=[],seen=new Set();while(g&&g!=='0'&&!seen.has(g)){seen.add(g);list.unshift(g);g=owner(map.get(ref(transforms.get(g),'m_Father'))||'');}return list;}
 const active=g=>ancestry(g).every(g=>field(map.get(g),'m_IsActive')!=='0');
 for(let g of gos)for(let m of g.matchAll(/component: \{fileID: (\d+)\}/g))if(owner(map.get(m[1])||'')!==id(g))errors.push(file+': component owner mismatch '+m[1]);
 for(let t of transforms.values()){let parent=ref(t,'m_Father');if(parent&&parent!=='0'){let pp=map.get(parent);if(!pp?.match(/m_Children:[\s\S]*?(?=  m_Father:)/)?.[0].includes('{fileID: '+id(t)+'}'))errors.push(file+': parent missing child '+id(t));}}
 for(let b of blocks){let type=b.match(/^--- !u!(\d+)/)[1],script=field(b,'m_Script')?.match(/guid: (\w+)/)?.[1];let kind=type==='212'?'SpriteRenderer':type==='23'?'MeshRenderer':type==='120'?'LineRenderer':script==='fe87c0e1cc204ed48ad3b37840f39efc'?'Image':script==='1344c3c82d62a2a41a3576d8abb8e3ea'?'RawImage':null;if(!kind)continue;
  let g=owner(b),chain=ancestry(g),tr=transforms.get(g);let asset=field(b,'m_Sprite')||field(b,'m_Texture')||'';let guid=asset.match(/guid: (\w+)/)?.[1];let parent=ref(tr,'m_Father'),siblings=map.get(parent)?.match(/m_Children:[\s\S]*?(?=  m_Father:)/)?.[0]||'';
  let canvas=chain.map(g=>blocks.find(b=>b.startsWith('--- !u!223 ')&&owner(b)===g)).filter(Boolean).at(-1);
  layers.push({scene:file,path:chain.map(goName).join('/'),kind,active:active(g)&&field(b,'m_Enabled')!=='0',order:Number(field(b,'m_SortingOrder')||0),canvasOrder:Number(field(canvas,'m_SortingOrder')||0),sibling:[...siblings.matchAll(/fileID: (\d+)/g)].findIndex(m=>m[1]===id(tr)),position:field(tr,'m_LocalPosition'),size:field(tr,'m_SizeDelta')||field(b,'m_Size'),scale:field(tr,'m_LocalScale'),raycast:field(b,'m_RaycastTarget'),asset:guid?assetGuids.get(guid):null});
 }
 let used=layers.filter(l=>l.scene===file),enabled=used.filter(l=>l.active);
 for(let l of enabled)if(l.kind==='Image'&&l.raycast==='1'&&!/Button|Continue|Next|Previous|Review|Listen|Clear|Title|Return|New Game|Step Out|Tape|Overlay|QTE|Test|Dialogue$|Start$/.test(l.path.split('/').at(-1)))warnings.push(file+': verify raycast image '+l.path);
 if(!build.includes('path: '+p))errors.push(file+': missing build entry');
 scenes.push({scene:file,objects:gos.length,layers:used.length,activeLayers:enabled.length,localReferences:'checked',assetReferences:'checked',hierarchy:'checked'});
}
const report={checkedAt:new Date().toISOString(),errors,warnings,scenes,layers};fs.mkdirSync('Logs',{recursive:true});fs.writeFileSync('Logs/FullProject-audit.json',JSON.stringify(report,null,2));fs.writeFileSync('Logs/FullProject-layers.tsv',['Scene\tObject\tType\tActive\tCanvasOrder\tSortOrder\tSibling\tPosition\tSize\tScale\tAsset',...layers.map(l=>[l.scene,l.path,l.kind,l.active,l.canvasOrder,l.order,l.sibling,l.position,l.size,l.scale,l.asset].join('\t'))].join('\n'));
console.log(JSON.stringify({scenes,errors,warnings,layerCount:layers.length},null,2));if(errors.length)process.exitCode=1;
