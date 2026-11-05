using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    [ExecuteAlways]
    public sealed class ReTuneSeatedVisitors : MonoBehaviour
    {
        public SpriteRenderer foregroundSource;
        public Camera sceneCamera;
        public Image deskForeground;
        public RectTransform clippingFrame;
        public Image roomBackground;
        public bool useLayeredRoom=true;
        public bool useSeparateDeskLayers=true;
        public Image deskCup;
        [Range(.1f,.5f)] public float deskOcclusionHeight=.26f;
        void LateUpdate(){AlignForeground();}
        public void AlignForeground()
        {
            if(deskForeground==null||clippingFrame==null)return;
            var rect=deskForeground.rectTransform;var parent=(RectTransform)clippingFrame.parent;var canvas=rect.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            Vector2 center=Vector2.zero,size;
            if(!useLayeredRoom&&foregroundSource!=null&&foregroundSource.sprite!=null&&sceneCamera!=null)
            {
                var sprite=foregroundSource.sprite;deskForeground.sprite=sprite;
                Vector2 localMin=-sprite.pivot/sprite.pixelsPerUnit,localMax=(sprite.rect.size-sprite.pivot)/sprite.pixelsPerUnit;
                Vector3 bottom=sceneCamera.WorldToScreenPoint(foregroundSource.transform.TransformPoint(localMin));
                Vector3 top=sceneCamera.WorldToScreenPoint(foregroundSource.transform.TransformPoint(localMax));
                Vector2 min,max;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,bottom,camera,out min);RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,top,camera,out max);
                center=(min+max)*.5f-parent.rect.center;size=new Vector2(Mathf.Abs(max.x-min.x),Mathf.Abs(max.y-min.y));
            }
            else
            {
                var sprite=deskForeground.sprite;if(sprite==null)return;float ratio=sprite.rect.width/sprite.rect.height;
                float width=Mathf.Max(parent.rect.width,parent.rect.height*ratio);size=new Vector2(width,width/ratio);
            }
            deskForeground.raycastTarget=false;deskForeground.preserveAspect=false;
            clippingFrame.anchorMin=clippingFrame.anchorMax=clippingFrame.pivot=new Vector2(.5f,.5f);
            float coverage=useSeparateDeskLayers?1:deskOcclusionHeight;
            clippingFrame.anchoredPosition=center-new Vector2(0,size.y*(1-coverage)*.5f);clippingFrame.sizeDelta=new Vector2(size.x,size.y*coverage);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=size;
            if(deskCup!=null){var cup=deskCup.rectTransform;cup.anchorMin=cup.anchorMax=cup.pivot=new Vector2(.5f,0);cup.anchoredPosition=Vector2.zero;cup.sizeDelta=size;deskCup.raycastTarget=false;deskCup.preserveAspect=false;}
            if(roomBackground!=null){var bg=roomBackground.rectTransform;bg.anchorMin=bg.anchorMax=bg.pivot=new Vector2(.5f,.5f);bg.anchoredPosition=center;bg.sizeDelta=size;roomBackground.raycastTarget=false;}
        }
    }
}
