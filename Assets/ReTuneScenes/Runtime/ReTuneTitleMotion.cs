using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    [ExecuteAlways]
    public sealed class ReTuneTitleMotion : MonoBehaviour
    {
        public RectTransform artwork;
        public RectTransform[] backgroundLayers;
        public RectTransform logo;
        public RectTransform[] menus;
        [Header("依次出现（秒）")]
        [Min(0)] public float entranceDelay=.15f;
        [Min(.05f)] public float fadeDuration=.75f;
        [Min(0)] public float menuStagger=.24f;
        [Header("鼠标悬停与柔光")]
        [Range(1,1.3f)] public float hoverScale=1.10f;
        [Min(.1f)] public float hoverResponse=10;
        [Min(.1f)] public float glowPeriod=3.6f;
        [Range(0,.2f)] public float glowBreath=.06f;
        [Header("大标题呼吸动画")]
        [Tooltip("上下浮动幅度，单位为画布像素；0 关闭浮动。")]
        [Min(0)] public float logoFloatAmplitude=4;
        [Tooltip("一轮呼吸动画的时长，单位为秒。")]
        [Min(.1f)] public float logoBreathPeriod=3.6f;
        [Tooltip("最大额外缩放量，0.01 表示最大放大到 101%。")]
        [Range(0,.05f)] public float logoScaleAmplitude=.01f;
        [Header("鼠标景深（画布像素，0 可关闭）")]
        public float[] layerMovement={1,2,5,10};
        [Min(.1f)] public float mouseResponse=5;
        [Range(1.01f,1.15f)] public float backgroundOverscan=1.04f;
        CanvasGroup[] fades;
        Text[] labels;
        ReTuneTextGlow[] glows;
        float[] hover;
        Vector2 pointer;
        float elapsed;
        void OnEnable()
        {
            FitBackground();
            if(!Application.isPlaying)return;
            elapsed=0;pointer=Vector2.zero;
            int count=menus==null?0:menus.Length;
            fades=new CanvasGroup[count+1];labels=new Text[count];glows=new ReTuneTextGlow[count];hover=new float[count];
            if(logo!=null)fades[0]=Fade(logo);
            for(int i=0;i<count;i++)
            {
                fades[i+1]=Fade(menus[i]);labels[i]=menus[i].GetComponentInChildren<Text>(true);
                if(labels[i]!=null)glows[i]=labels[i].GetComponent<ReTuneTextGlow>();
            }
            Tick(0);
        }
        static CanvasGroup Fade(RectTransform target)
        {
            var group=target.GetComponent<CanvasGroup>();return group!=null?group:target.gameObject.AddComponent<CanvasGroup>();
        }
        void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
        void LateUpdate(){FitBackground();}
        void FitBackground()
        {
            if(artwork==null||backgroundLayers==null)return;
            var area=artwork.rect;
            foreach(var layer in backgroundLayers)
            {
                if(layer==null)continue;var image=layer.GetComponent<Image>();if(image==null||image.sprite==null)continue;
                float ratio=image.sprite.rect.width/image.sprite.rect.height;
                float width=Mathf.Max(area.width,area.height*ratio)*backgroundOverscan;
                layer.anchorMin=layer.anchorMax=layer.pivot=new Vector2(.5f,.5f);layer.sizeDelta=new Vector2(width,width/ratio);
                if(!Application.isPlaying)layer.anchoredPosition=Vector2.zero;
            }
        }
        void Tick(float delta)
        {
            elapsed+=delta;
            Vector2 mouse=Input.mousePosition;
            bool inside=Application.isFocused&&mouse.x>=0&&mouse.y>=0&&mouse.x<=Screen.width&&mouse.y<=Screen.height;
            Vector2 target=inside?new Vector2(mouse.x/Mathf.Max(1,Screen.width)*2-1,mouse.y/Mathf.Max(1,Screen.height)*2-1):Vector2.zero;
            pointer=Vector2.Lerp(pointer,target,1-Mathf.Exp(-mouseResponse*delta));
            if(backgroundLayers!=null)for(int i=0;i<backgroundLayers.Length;i++)if(backgroundLayers[i]!=null)
                backgroundLayers[i].anchoredPosition=-pointer*(layerMovement!=null&&i<layerMovement.Length?layerMovement[i]:0);
            if(fades==null)return;
            float pulse=Mathf.Sin(elapsed*Mathf.PI*2/Mathf.Max(.1f,glowPeriod));
            for(int i=0;i<fades.Length;i++)
            {
                if(fades[i]==null)continue;
                float start=entranceDelay+(i==0?0:fadeDuration*.55f+(i-1)*menuStagger);
                float t=Mathf.Clamp01((elapsed-start)/Mathf.Max(.05f,fadeDuration));float eased=t*t*(3-2*t);
                fades[i].alpha=eased;fades[i].blocksRaycasts=t>=.95f;
                if(i==0)
                {
                    if(logo!=null)
                    {
                        // Start the loop after the entrance and blend it in smoothly.
                        float idleTime=Mathf.Max(0,elapsed-start-fadeDuration);
                        float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(idleTime/.6f));
                        float phase=idleTime*Mathf.PI*2/Mathf.Max(.1f,logoBreathPeriod);
                        float breath=(1-Mathf.Cos(phase))*.5f;
                        logo.anchoredPosition=new Vector2(0,-12*(1-eased)+Mathf.Sin(phase)*logoFloatAmplitude*blend);
                        logo.localScale=Vector3.one*(1+breath*logoScaleAmplitude*blend);
                        var image=logo.GetComponent<Image>();
                        if(image!=null)image.color=new Color(1,1,1,1-glowBreath+glowBreath*breath);
                    }
                    continue;
                }
                int n=i-1;var label=labels[n];if(label==null)continue;
                var canvas=menus[n].GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
                bool over=inside&&t>=.95f&&RectTransformUtility.RectangleContainsScreenPoint(menus[n],mouse,camera);
                hover[n]=Mathf.Lerp(hover[n],over?1:0,1-Mathf.Exp(-hoverResponse*delta));
                label.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,hoverScale,hover[n]);
                label.rectTransform.anchoredPosition=new Vector2(0,-10*(1-eased));
                var glow=glows[n];if(glow!=null)
                {
                    glow.radius=Mathf.Lerp(5,8,hover[n]);
                    glow.glowColor=new Color(1,.72f,.22f,.14f+glowBreath*.3f*pulse+.10f*hover[n]);label.SetVerticesDirty();
                }
            }
        }
    }
}
