using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    // Positions use the cover's actual drawn rectangle, including aspect-ratio margins.
    [ExecuteAlways]
    public sealed class ReTuneTitleHotspots : MonoBehaviour
    {
        public Image cover;
        public RectTransform newGame, review, tape;
        void LateUpdate(){Align();}
        public void Align()
        {
            if(cover==null||cover.sprite==null)return;
            Rect area=cover.rectTransform.rect;
            float ratio=cover.sprite.rect.width/cover.sprite.rect.height;
            if(area.width/area.height>ratio){float width=area.height*ratio;area.x+=(area.width-width)/2;area.width=width;}
            else {float height=area.width/ratio;area.y+=(area.height-height)/2;area.height=height;}
            Place(newGame,area,new Rect(.69f,.11f,.27f,.14f));
            Place(review,area,new Rect(.75f,.26f,.21f,.14f));
            Place(tape,area,new Rect(.69f,.37f,.20f,.13f));
        }
        static void Place(RectTransform target,Rect area,Rect region)
        {
            if(target==null)return;
            target.anchorMin=target.anchorMax=target.pivot=new Vector2(.5f,.5f);
            Vector2 parentCenter=((RectTransform)target.parent).rect.center;
            target.anchoredPosition=new Vector2(area.x+area.width*region.center.x,area.yMax-area.height*region.center.y)-parentCenter;
            target.sizeDelta=new Vector2(area.width*region.width,area.height*region.height);
        }
    }
}
