using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    // 表盘弧形判定区，直接绘制 UI 网格，不需要额外美术贴图。
    public sealed class ReTuneDialZone : MaskableGraphic
    {
        public float centerAngle=90, halfWidth=18, innerRadius=276, outerRadius=282;
        public void SetRange(float angle,float width,Color tint){centerAngle=angle;halfWidth=width;color=tint;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();const int segments=32;
            for(int i=0;i<=segments;i++)
            {
                float angle=(centerAngle-halfWidth+halfWidth*2*i/segments)*Mathf.Deg2Rad;
                var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                mesh.AddVert(direction*innerRadius,color,Vector2.zero);mesh.AddVert(direction*outerRadius,color,Vector2.one);
                if(i>0){int n=i*2;mesh.AddTriangle(n-2,n-1,n);mesh.AddTriangle(n,n-1,n+1);}
            }
        }
    }
}

