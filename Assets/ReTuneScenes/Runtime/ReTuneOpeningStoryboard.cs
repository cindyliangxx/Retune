using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    // 开场分镜只负责美术切换；叙事翻页和标题跳转仍由 NarrativePlayer 管理。
    [ExecuteAlways]
    public sealed class ReTuneOpeningStoryboard : MonoBehaviour
    {
        public GameObject[] shots;
        public Text letterText;
        public Image dialoguePanel;
        public Text narrativeBody;
        public AudioSource sound;
        public AudioClip unlockSound, doorSound, openSound;
        [Header("分镜动画时长（秒，越小越快）")]
        [Tooltip("左上方块和中间绿色方块滑入所需的时间。默认 0.85 秒。")]
        [Min(.1f)] public float slideDuration=.85f;
        [Tooltip("第三张底部手部图层从透明到完全显示所需的时间。默认 1.2 秒。")]
        [Min(.1f)] public float handFadeDuration=1.2f;
        [Header("电影字幕")]
        [Min(40)] public float subtitleBarHeight=76;
        [Min(16)] public int subtitleFontSize=38;
        [Tooltip("每秒显示的字符数，越大打字越快。")]
        [Min(1)] public float charactersPerSecond=32;
        [Tooltip("标点后的额外停顿时间（秒）。")]
        [Min(0)] public float punctuationPause=.16f;
        public Color subtitleBackdropColor=new Color(.24f,.17f,.10f,.72f);
        [Header("信纸排版（跟随信纸缩放）")]
        [Min(16)] public float letterFontSize=48;
        public Text[] letterLines;
        RectTransform letterPaper;
        readonly List<string> subtitleCues=new List<string>();
        int subtitleIndex;
        string activeSubtitle="";
        int visibleCharacters;
        float typingClock;
        public bool IsTyping { get { return visibleCharacters<activeSubtitle.Length; } }
        int current=-1;
        float age;
        RectTransform movingLayer;
        CanvasGroup fadingLayer;
        Vector2 slideOffset;
        float transitionDuration;
        void OnEnable(){EnsureSubtitleBackdrop();EnsureLetterLayout();ConfigurePresentation();}
        void OnValidate(){ConfigurePresentation();}
        void LateUpdate(){FitArtwork();FitLetterLines();}
        public void EnsureLetterLayout()
        {
            if(letterText==null||shots==null||shots.Length<=6||shots[6]==null)return;
            var paper=shots[6].GetComponentInChildren<Image>(true);if(paper==null)return;
            letterPaper=paper.rectTransform;
            string[] lines={"Dear friend,","When this letter reaches you,","I'll probably be gone. Thanks","for taking the shop from me.","Promise you'll keep it going."};
            letterText.rectTransform.SetParent(letterPaper,false);
            letterLines=new Text[lines.Length];letterLines[0]=letterText;
            for(int i=0;i<lines.Length;i++)
            {
                Text line=letterText;
                if(i>0)
                {
                    var child=letterPaper.Find("Letter Line "+(i+1));
                    if(child==null){var go=new GameObject("Letter Line "+(i+1),typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.layer=5;go.transform.SetParent(letterPaper,false);child=go.transform;}
                    line=child.GetComponent<Text>();
                }
                letterLines[i]=line;line.font=letterText.font;line.color=new Color(.3f,.22f,.14f);line.raycastTarget=false;
                line.text=lines[i];line.alignment=TextAnchor.UpperLeft;line.resizeTextForBestFit=false;
                line.horizontalOverflow=HorizontalWrapMode.Overflow;line.verticalOverflow=VerticalWrapMode.Overflow;
            }
            FitLetterLines();
        }
        void FitLetterLines()
        {
            if(letterPaper==null||letterLines==null)return;
            float scale=letterPaper.rect.width/1920f;
            // Baselines follow the five hand-drawn stave lines in LetterPaper.png.
            float[] baselines={415,500,590,680,735};
            for(int i=0;i<letterLines.Length&&i<baselines.Length;i++)
            {
                var line=letterLines[i];if(line==null)continue;
                line.fontSize=Mathf.Max(16,Mathf.RoundToInt(letterFontSize*scale));
                var rect=line.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(575*scale,-(baselines[i]-letterFontSize*.95f)*scale);
                rect.sizeDelta=new Vector2(900*scale,letterFontSize*2*scale);
            }
        }
        public void EnsureSubtitleBackdrop()
        {
            if(dialoguePanel!=null&&dialoguePanel.GetComponent<ReTuneSubtitleGradient>()==null)dialoguePanel.gameObject.AddComponent<ReTuneSubtitleGradient>();
        }
        public void ConfigurePresentation()
        {
            if(dialoguePanel!=null){dialoguePanel.color=subtitleBackdropColor;dialoguePanel.raycastTarget=false;}
            var flow=GetComponent<ReTuneNarrativePlayer>();
            if(flow!=null)
            {
                if(flow.panel!=null)
                {
                    var panel=(RectTransform)flow.panel.transform;
                    panel.anchorMin=Vector2.zero;panel.anchorMax=new Vector2(1,0);panel.pivot=new Vector2(.5f,0);
                    panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(0,subtitleBarHeight);
                }
                if(narrativeBody!=null)
                {
                    var body=narrativeBody.rectTransform;body.anchorMin=Vector2.zero;body.anchorMax=Vector2.one;body.pivot=new Vector2(.5f,.5f);
                    body.anchoredPosition=Vector2.zero;body.sizeDelta=new Vector2(-64,-12);
                    narrativeBody.color=new Color(1,.96f,.86f);narrativeBody.fontSize=subtitleFontSize;narrativeBody.alignment=TextAnchor.MiddleCenter;
                    narrativeBody.supportRichText=true;
                    narrativeBody.resizeTextForBestFit=false;narrativeBody.horizontalOverflow=HorizontalWrapMode.Overflow;
                }
                if(flow.nextButton!=null)
                {
                    var next=(RectTransform)flow.nextButton.transform;next.anchorMin=next.anchorMax=next.pivot=Vector2.one;next.anchoredPosition=new Vector2(-30,-150);
                }
                if(flow.pageLabel!=null)
                {
                    var page=flow.pageLabel.rectTransform;page.anchorMin=page.anchorMax=new Vector2(.5f,1);page.pivot=new Vector2(.5f,1);page.anchoredPosition=new Vector2(0,-150);
                }
                foreach(var button in new[]{flow.backButton,flow.nextButton})
                {
                    if(button==null)continue;
                    var background=button.GetComponent<Image>();if(background!=null)background.color=Color.clear;
                    button.transition=Selectable.Transition.None;
                    button.gameObject.SetActive(false);
                }
                if(flow.pageLabel!=null)flow.pageLabel.gameObject.SetActive(false);
            }
            FitArtwork();
        }
        void FitArtwork()
        {
            if(shots==null)return;
            foreach(var shot in shots)
            {
                if(shot==null)continue;
                var parent=shot.transform as RectTransform;if(parent==null)continue;
                float width=parent.rect.width,height=parent.rect.height;if(width<=0||height<=0)continue;
                foreach(Transform child in parent)
                {
                    var image=child.GetComponent<Image>();if(image==null||image.sprite==null)continue;
                    float ratio=image.sprite.rect.width/image.sprite.rect.height;
                    // Cover the viewport without distorting the art. Full-canvas layers
                    // share the same crop, so their original alignment stays intact.
                    var rect=image.rectTransform;image.preserveAspect=false;
                    rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);
                    rect.sizeDelta=new Vector2(Mathf.Max(width,height*ratio)-width,Mathf.Max(height,width/ratio)-height);
                }
            }
        }
        public bool IsTransitioning { get { return transitionDuration>0 && age<transitionDuration; } }
        public void CompleteTransition()
        {
            age=Mathf.Max(age,transitionDuration);ApplyTransition();
        }
        public void Show(int page)
        {
            if(current==page)return;current=page;age=0;
            movingLayer=null;fadingLayer=null;transitionDuration=0;
            for(int i=0;i<shots.Length;i++)if(shots[i]!=null)
            {
                shots[i].SetActive(i==page);shots[i].transform.localScale=Vector3.one;
                foreach(Transform layer in shots[i].transform)
                {
                    var rect=layer as RectTransform;if(rect!=null)rect.anchoredPosition=Vector2.zero;
                    var opacity=layer.GetComponent<CanvasGroup>();if(opacity!=null)opacity.alpha=1;
                }
            }
            // The PNGs retain their full canvas: animate their offsets back to zero.
            if(page>=1&&page<=3&&shots[page]!=null)
            {
                var shot=(RectTransform)shots[page].transform;
                int childIndex=page==2?1:0;
                if(shot.childCount>childIndex)
                {
                    var layer=(RectTransform)shot.GetChild(childIndex);
                    if(page<3)
                    {
                        movingLayer=layer;
                        slideOffset=page==1?new Vector2(0,Mathf.Max(720,shot.rect.height)*1.1f):new Vector2(-Mathf.Max(1280,shot.rect.width)*1.1f,0);
                        transitionDuration=slideDuration;
                    }
                    else
                    {
                        fadingLayer=layer.GetComponent<CanvasGroup>();if(fadingLayer==null)fadingLayer=layer.gameObject.AddComponent<CanvasGroup>();
                        transitionDuration=handFadeDuration;
                    }
                    ApplyTransition();
                }
            }
            bool reading=page==6;
            letterText.gameObject.SetActive(reading);narrativeBody.gameObject.SetActive(true);
            dialoguePanel.color=subtitleBackdropColor;
            PrepareSubtitles(narrativeBody.text);
            if(page==8)ReTuneBackgroundMusic.StartOpeningMusic();
            if(sound!=null){sound.Stop();var clip=page==2?unlockSound:page==3?doorSound:page==8?openSound:null;if(clip!=null)sound.PlayOneShot(clip,.5f);}
        }
        void PrepareSubtitles(string text)
        {
            subtitleCues.Clear();subtitleIndex=0;
            Canvas.ForceUpdateCanvases();
            var settings=narrativeBody.GetGenerationSettings(Vector2.zero);settings.horizontalOverflow=HorizontalWrapMode.Overflow;
            float width=Mathf.Max(200,narrativeBody.rectTransform.rect.width-8);
            string cue="";
            foreach(string word in text.Split(new[]{' ','\n','\r'},System.StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate=cue.Length==0?word:cue+" "+word;
                float measured=narrativeBody.cachedTextGeneratorForLayout.GetPreferredWidth(candidate,settings)/narrativeBody.pixelsPerUnit;
                if(cue.Length>0&&measured>width){subtitleCues.Add(cue);cue=word;}else cue=candidate;
                if(cue.EndsWith(".")||cue.EndsWith("?")||cue.EndsWith("!")){subtitleCues.Add(cue);cue="";}
            }
            if(cue.Length>0)subtitleCues.Add(cue);
            BeginSubtitle(subtitleCues.Count>0?subtitleCues[0]:"");
        }
        public bool AdvanceSubtitle()
        {
            if(subtitleIndex+1>=subtitleCues.Count)return false;
            BeginSubtitle(subtitleCues[++subtitleIndex]);return true;
        }
        void BeginSubtitle(string text)
        {
            activeSubtitle=text;visibleCharacters=Application.isPlaying?0:text.Length;typingClock=0;RenderSubtitle();
        }
        public void CompleteSubtitle(){visibleCharacters=activeSubtitle.Length;typingClock=0;RenderSubtitle();}
        void RenderSubtitle()
        {
            // Transparent trailing glyphs preserve the final line's centered layout.
            narrativeBody.text=IsTyping?activeSubtitle.Substring(0,visibleCharacters)+"<color=#FFFFFF00>"+activeSubtitle.Substring(visibleCharacters)+"</color>":activeSubtitle;
        }
        void UpdateTyping(float delta)
        {
            if(!IsTyping)return;
            typingClock+=delta;bool changed=false;
            while(IsTyping)
            {
                float wait=1/Mathf.Max(1,charactersPerSecond);
                if(visibleCharacters>0&&".,!?;:".IndexOf(activeSubtitle[visibleCharacters-1])>=0)wait+=punctuationPause;
                if(typingClock<wait)break;
                typingClock-=wait;visibleCharacters++;changed=true;
            }
            if(changed)RenderSubtitle();
        }
        void Update()
        {
            if(!Application.isPlaying)return;
            if(current<0||current>=shots.Length||shots[current]==null)return;
            age+=Time.deltaTime;
            UpdateTyping(Time.unscaledDeltaTime);
            ApplyTransition();
            // 极轻微的镜头推近，避免改变图层之间原有的拼接位置。
            if(current==0||current==4||current==5||current==8)shots[current].transform.localScale=Vector3.one*Mathf.Lerp(1,1.025f,Mathf.Clamp01(age/5));
        }
        void ApplyTransition()
        {
            float t=transitionDuration>0?Mathf.Clamp01(age/transitionDuration):1;
            float eased=t*t*(3-2*t);
            if(movingLayer!=null)movingLayer.anchoredPosition=slideOffset*(1-eased);
            if(fadingLayer!=null)fadingLayer.alpha=eased;
        }
    }
}

