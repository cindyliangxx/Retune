using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReTune.Scenes
{
    [ExecuteAlways, DefaultExecutionOrder(200)]
    public sealed class ReTuneUITheme : MonoBehaviour
    {
        public static readonly Color Ink=new Color(1,.96f,.86f,1);
        public static readonly Color Accent=new Color(1,.79f,.43f,1);
        public static readonly Color Veil=new Color(.12f,.09f,.07f,.42f);
        public static readonly Color Surface=new Color(.17f,.12f,.09f,.64f);
        ReTuneSceneController controller;
        Coroutine fade;
        void OnEnable(){Apply();}
        void Start(){Apply();}
        public void Apply()
        {
            controller=GetComponent<ReTuneSceneController>();if(controller==null)return;
            foreach(var root in gameObject.scene.GetRootGameObjects())
                foreach(var button in root.GetComponentsInChildren<Button>(true))
                {
                    if(button.name=="QTE_Note"||button.GetComponentInParent<ReTuneTitleMotion>()!=null)continue;
                    StyleButton(button);
                }
            if(controller.overlay==null)return;
            var shield=controller.overlay.GetComponent<Image>();if(shield!=null){shield.color=Veil;shield.raycastTarget=true;}
            var title=controller.overlayTitle;var body=controller.overlayBody;
            if(title==null||body==null)return;
            var panel=title.transform.parent as RectTransform;
            if(panel==null)return;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);
            panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(1160,540);
            var image=panel.GetComponent<Image>();
            if(image!=null){image.color=Surface;image.raycastTarget=false;if(image.GetComponent<ReTuneSubtitleGradient>()==null)image.gameObject.AddComponent<ReTuneSubtitleGradient>();}
            Place(title.rectTransform,48,64,64);title.fontSize=42;title.color=Accent;title.alignment=TextAnchor.UpperLeft;title.raycastTarget=false;
            Place(body.rectTransform,136,64,310);body.fontSize=36;body.resizeTextForBestFit=false;body.lineSpacing=1.35f;body.text=SentenceLines(body.text);
            body.alignment=TextAnchor.UpperLeft;body.horizontalOverflow=HorizontalWrapMode.Wrap;body.verticalOverflow=VerticalWrapMode.Truncate;body.color=Ink;body.raycastTarget=false;
            foreach(var button in panel.GetComponentsInChildren<Button>(true))
            {
                StyleButton(button);var rect=(RectTransform)button.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);
                rect.anchoredPosition=new Vector2(-64,34);rect.sizeDelta=new Vector2(240,56);
            }
        }
        static void Place(RectTransform rect,float top,float inset,float height)
        {
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-top);rect.sizeDelta=new Vector2(-inset*2,height);
        }
        public static string SentenceLines(string text)
        {
            return string.IsNullOrEmpty(text)?text:System.Text.RegularExpressions.Regex.Replace(text,@"(?<=[.!?])\s+(?=[A-Z])","\n");
        }
        public static void StyleButton(Button button)
        {
            var label=button.GetComponentInChildren<Text>(true);if(label==null)return;
            var image=button.GetComponent<Image>();if(image!=null){image.color=Color.clear;image.raycastTarget=true;}
            label.color=Ink;label.raycastTarget=false;
            button.targetGraphic=label;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Accent;colors.selectedColor=Accent;colors.pressedColor=new Color(.82f,.64f,.38f);colors.disabledColor=new Color(.5f,.47f,.42f,.5f);colors.fadeDuration=.15f;button.colors=colors;
            var shadow=label.GetComponent<Shadow>();if(shadow==null)shadow=label.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color(.08f,.05f,.03f,.65f);shadow.effectDistance=new Vector2(1,-2);shadow.useGraphicAlpha=true;
        }
        public void RevealOverlay()
        {
            if(!Application.isPlaying||controller==null||controller.overlay==null)return;
            if(fade!=null)StopCoroutine(fade);
            var group=controller.overlay.GetComponent<CanvasGroup>();if(group==null)group=controller.overlay.AddComponent<CanvasGroup>();
            fade=StartCoroutine(FadeIn(group));
        }
        IEnumerator FadeIn(CanvasGroup group)
        {
            group.alpha=0;
            for(float t=0;t<.18f;t+=Time.unscaledDeltaTime){group.alpha=Mathf.SmoothStep(0,1,t/.18f);yield return null;}
            group.alpha=1;fade=null;
        }
    }
}
