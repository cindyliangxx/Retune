const fs=require('fs');const m=JSON.parse(fs.readFileSync('Temp/winding-key-mask.json','utf8').replace(/^\uFEFF/,''));
let verts=[],uvs=[],faces=[],map=new Map();const depth=.11;
function vert(x,y,z){let k=[x,y,z].join(',');if(map.has(k))return map.get(k);let id=verts.length+1;map.set(k,id);verts.push(`v ${((x-326)/100).toFixed(5)} ${((156-y)/100).toFixed(5)} ${z}`);uvs.push(`vt ${(x/m.width).toFixed(7)} ${(1-y/m.height).toFixed(7)}`);return id;}
function face(a,b,c,d){faces.push(`f ${a}/${a} ${b}/${b} ${c}/${c}`,`f ${a}/${a} ${c}/${c} ${d}/${d}`);}
const filled=(x,y)=>y>=0&&y<m.rows.length&&x>=0&&x<m.rows[0].length&&m.rows[y][x]==='1';
for(let y=0;y<m.rows.length;y++)for(let x=0;x<m.rows[y].length;x++)if(filled(x,y)){
 let X=x*2,Y=y*2;
 let a=vert(X,Y,-depth),b=vert(X+2,Y,-depth),c=vert(X+2,Y+2,-depth),d=vert(X,Y+2,-depth);
 let A=vert(X,Y,depth),B=vert(X+2,Y,depth),C=vert(X+2,Y+2,depth),D=vert(X,Y+2,depth);
 face(a,b,c,d);face(D,C,B,A);
 if(!filled(x,y-1))face(A,B,b,a);
 if(!filled(x+1,y))face(B,C,c,b);
 if(!filled(x,y+1))face(C,D,d,c);
 if(!filled(x-1,y))face(D,A,a,d);
}
fs.writeFileSync('Assets/ReTuneScenes/Art/WindingKey/Model/winding-key.obj',['mtllib winding-key.mtl','o WindingKey','usemtl AgedMetal','s off',...verts,...uvs,...faces,''].join('\n'));
let p='Assets/ReTuneScenes/Art/WindingKey/Model/winding-key.obj.meta';let meta=fs.readFileSync(p,'utf8');meta=meta.replace('indexFormat: 0','indexFormat: 1');fs.writeFileSync(p,meta);
p='Assets/ReTuneScenes/Scenes/02_Parts.unity';let s=fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n');let bs=s.split(/(?=^--- !u!)/m);function ch(id,fn){let i=bs.findIndex(b=>new RegExp('^--- !u!\\d+ &'+id+'\\n').test(b));if(i<0)throw Error(id);bs[i]=fn(bs[i]);}
ch('5751941348503557548',b=>b.replace('m_SortingOrder: 10','m_SortingOrder: 1'));
ch('6796390753169029418',b=>b.replace(/m_LocalRotation: \{[^\n]+\}/,'m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}').replace(/m_LocalPosition: \{[^\n]+\}/,'m_LocalPosition: {x: -3.47, y: 0.26, z: 0.25}').replace(/m_LocalEulerAnglesHint: \{[^\n]+\}/,'m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}'));
ch('6206405945810913394',b=>b.replace(/m_LocalPosition: \{[^\n]+\}/,'m_LocalPosition: {x: 0, y: 0, z: 0}'));
fs.writeFileSync(p,bs.join(''));
p='Assets/ReTuneScenes/Art/WindingKey/Model/WindingKeyMetal.mat';s=fs.readFileSync(p,'utf8');s=s.replace('m_TexEnvs: []','m_TexEnvs:\n    - _BaseMap:\n        m_Texture: {fileID: 2800000, guid: 2ff19071ed9893f43b5ec791549e9c20, type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}').replace('_BaseColor: {r: 0.36, g: 0.30, b: 0.28, a: 1}','_BaseColor: {r: 1, g: 1, b: 1, a: 1}');fs.writeFileSync(p,s);
console.log(`Winding key rebuilt from original silhouette: ${verts.length} vertices, ${faces.length} triangles; layered behind roller.`);
