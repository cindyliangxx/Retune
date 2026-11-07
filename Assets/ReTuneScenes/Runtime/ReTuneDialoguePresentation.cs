using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    [ExecuteAlways]
    public sealed class ReTuneDialoguePresentation : MonoBehaviour
    {
        public ReTuneNarrativePlayer player;
        [Header("悬浮对话框")]
        [Min(140)] public float panelHeight=184;
        [Min(20)] public int dialogueFontSize=40;
        public Color backgroundColor=new Color(.22f,.16f,.10f,.78f);
        [Header("逐字出现")]
        [Min(1)] public float charactersPerSecond=32;
        [Min(0)] public float punctuationPause=.16f;
        readonly List<string> cues=new List<string>();
        int cueIndex,visible;
        float clock;
        string fullText="";
        public bool IsTyping { get { return visible<fullText.Length; } }
        void OnEnable(){Configure();}
        public void Configure()
        {
            if(player==null)player=GetComponent<ReTuneNarrativePlayer>();if(player==null)return;
            if(player.mode==ReTuneNarrativePlayer.Mode.Ending)return;
            var panel=(RectTransform)player.panel.transform;
            panel.anchorMin=Vector2.zero;panel.anchorMax=new Vector2(1,0);panel.pivot=new Vector2(.5f,0);
            panel.anchoredPosition=new Vector2(0,24);panel.sizeDelta=new Vector2(-96,panelHeight);
            var image=player.panel.GetComponent<Image>();image.color=backgroundColor;image.raycastTarget=false;
            if(image.GetComponent<ReTuneSubtitleGradient>()==null)image.gameObject.AddComponent<ReTuneSubtitleGradient>();
            Place(player.heading.rectTransform,12,-64,42);player.heading.fontSize=30;player.heading.alignment=TextAnchor.MiddleLeft;
            Place(player.body.rectTransform,56,-64,panelHeight-72);player.body.fontSize=dialogueFontSize;player.body.alignment=TextAnchor.UpperLeft;
            player.body.resizeTextForBestFit=false;player.body.supportRichText=true;player.body.horizontalOverflow=HorizontalWrapMode.Wrap;
            HideNavigation();
            // Retain the existing title action as a small floating text button.
            var canvas=panel.GetComponentInParent<Canvas>();
            foreach(var button in canvas.GetComponentsInChildren<Button>(true))
            {
                if(button.name!="Title")continue;
                var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;
                rect.anchoredPosition=new Vector2(-340,-20);rect.sizeDelta=new Vector2(150,52);
                var background=button.GetComponent<Image>();if(background!=null)background.color=Color.clear;
                button.transition=Selectable.Transition.None;
                var label=button.GetComponentInChildren<Text>(true);if(label!=null){label.fontSize=30;label.color=new Color(1,.96f,.86f);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;}
            }
        }
        static void Place(RectTransform rect,float top,float width,float height)
        {
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-top);rect.sizeDelta=new Vector2(width,height);
        }
        public void HideNavigation()
        {
            player.backButton.gameObject.SetActive(false);player.nextButton.gameObject.SetActive(false);player.pageLabel.gameObject.SetActive(false);
        }
        public void Show(string text)
        {
            if(player!=null&&player.mode==ReTuneNarrativePlayer.Mode.Ending)return;
            Configure();
            string name="Narration";Color ink=new Color(.96f,.92f,.82f);Color accent=new Color(.87f,.81f,.67f);
            string[] prefixes={"Tuner:","Postman:","Guitarist:","Owner:","Photographer:","Michael:"};
            string[] names={"You","Postman","Guitarist","Bookshop Owner","Michael","Michael"};
            for(int i=0;i<prefixes.Length;i++)if(text.StartsWith(prefixes[i],System.StringComparison.Ordinal))
            {
                name=names[i];text=text.Substring(prefixes[i].Length).Trim();
                ink=i==0?new Color(.83f,.96f,.91f):new Color(1,.95f,.82f);
                accent=i==0?new Color(.57f,.87f,.79f):new Color(1,.79f,.43f);break;
            }
            player.heading.text=name;player.heading.color=accent;player.body.color=ink;
            cues.Clear();cueIndex=0;Canvas.ForceUpdateCanvases();
            var body=player.body;var settings=body.GetGenerationSettings(new Vector2(body.rectTransform.rect.width,0));
            settings.verticalOverflow=VerticalWrapMode.Overflow;
            string cue="";
            foreach(string word in text.Split(new[]{' ','\n','\r'},System.StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate=cue.Length==0?word:cue+" "+word;
                float height=body.cachedTextGeneratorForLayout.GetPreferredHeight(candidate,settings)/body.pixelsPerUnit;
                if(cue.Length>0&&height>body.rectTransform.rect.height-2){cues.Add(cue);cue=word;}else cue=candidate;
            }
            if(cue.Length>0)cues.Add(cue);BeginCue(cues.Count>0?cues[0]:"");
        }
        void BeginCue(string text){fullText=text;visible=Application.isPlaying?0:text.Length;clock=0;RenderText();}
        void RenderText(){player.body.text=IsTyping?fullText.Substring(0,visible)+"<color=#FFFFFF00>"+fullText.Substring(visible)+"</color>":fullText;}
        public bool ConsumeNext()
        {
            if(IsTyping){visible=fullText.Length;RenderText();return true;}
            if(cueIndex+1>=cues.Count)return false;
            BeginCue(cues[++cueIndex]);return true;
        }
        public bool CanClickContinue()
        {
            var canvas=player.panel.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            foreach(var button in canvas.GetComponentsInChildren<Button>(false))
                if(button.interactable&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform,Input.mousePosition,camera))return false;
            return true;
        }
        void Update()
        {
            if(!Application.isPlaying||player==null||!IsTyping||player.controller!=null&&player.controller.PartsAnimationPaused)return;
            clock+=Time.unscaledDeltaTime;bool changed=false;
            while(IsTyping)
            {
                float wait=1/Mathf.Max(1,charactersPerSecond);
                if(visible>0&&".,!?;:".IndexOf(fullText[visible-1])>=0)wait+=punctuationPause;
                if(clock<wait)break;clock-=wait;visible++;changed=true;
            }
            if(changed)RenderText();
        }
    }
}
