using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    // 复用文字网格生成多层柔光，不需要修改字体文件或绘制新贴图。
    [RequireComponent(typeof(Text))]
    public sealed class ReTuneTextGlow : BaseMeshEffect
    {
        public Color glowColor=new Color(1,.72f,.22f,.12f);
        [Range(0,12)] public float radius=5;
        readonly List<UIVertex> original=new List<UIVertex>();
        public override void ModifyMesh(VertexHelper helper)
        {
            if(!IsActive()||radius<=0)return;
            original.Clear();helper.GetUIVertexStream(original);helper.Clear();
            const int directions=12,rings=3;
            // 先绘制外围柔光，再绘制原文字，保证字形清晰。
            for(int ring=rings;ring>=1;ring--)for(int direction=0;direction<directions;direction++)
            {
                float a=direction*Mathf.PI*2/directions;Vector3 offset=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius*ring/rings;
                Color color=glowColor;color.a*=1-(float)(ring-1)/rings;
                for(int i=0;i<original.Count;i++){var vertex=original[i];vertex.position+=offset;Color tint=color;tint.a*=vertex.color.a/255f;vertex.color=tint;helper.AddVert(vertex);}
            }
            for(int i=0;i<original.Count;i++)helper.AddVert(original[i]);
            for(int i=0;i<helper.currentVertCount;i+=3)helper.AddTriangle(i,i+1,i+2);
        }
    }
}
