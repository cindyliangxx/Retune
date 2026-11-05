using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    // A soft UI mesh, with no additional texture assets.
    [RequireComponent(typeof(Image))]
    public sealed class ReTuneSubtitleGradient : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper helper)
        {
            if(!IsActive()||helper.currentVertCount==0)return;
            UIVertex source=UIVertex.simpleVert;helper.PopulateUIVertex(ref source,0);
            Rect rect=((RectTransform)transform).rect;
            float[] x={0,.06f,.94f,1},y={0,.22f,.78f,1},opacity={0,1,1,0};
            helper.Clear();
            for(int row=0;row<3;row++)for(int column=0;column<3;column++)
            {
                var quad=new UIVertex[4];
                int[] dx={0,0,1,1},dy={0,1,1,0};
                for(int corner=0;corner<4;corner++)
                {
                    int ix=column+dx[corner],iy=row+dy[corner];var vertex=source;
                    vertex.position=new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,x[ix]),Mathf.Lerp(rect.yMin,rect.yMax,y[iy]),0);
                    var color=source.color;color.a=(byte)(color.a*opacity[ix]*opacity[iy]);vertex.color=color;quad[corner]=vertex;
                }
                helper.AddUIVertexQuad(quad);
            }
        }
    }
}
