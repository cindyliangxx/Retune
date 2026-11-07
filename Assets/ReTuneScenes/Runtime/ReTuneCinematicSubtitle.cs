using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    // Same cinematic typography and cue rules as Opening.
    public sealed class ReTuneCinematicSubtitle
    {
        readonly Text body;
        readonly List<string> cues = new List<string>();
        int cue, visible;
        string text = "";
        float clock;
        public bool IsTyping => visible < text.Length;
        public ReTuneCinematicSubtitle(Text target) { body = target; }
        public static void Layout(ReTuneNarrativePlayer flow, float height = 76, int fontSize = 38)
        {
            var panel = (RectTransform)flow.panel.transform;
            panel.anchorMin = Vector2.zero; panel.anchorMax = new Vector2(1, 0); panel.pivot = new Vector2(.5f, 0);
            panel.anchoredPosition = Vector2.zero; panel.sizeDelta = new Vector2(0, height);
            var image = panel.GetComponent<Image>();
            if (image != null) { image.color = new Color(.24f,.17f,.10f,.72f); image.raycastTarget = false; if(image.GetComponent<ReTuneSubtitleGradient>()==null)image.gameObject.AddComponent<ReTuneSubtitleGradient>(); }
            var rect = flow.body.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(-64,-12);
            flow.body.color = new Color(1,.96f,.86f); flow.body.fontSize = fontSize; flow.body.alignment = TextAnchor.MiddleCenter;
            flow.body.supportRichText = true; flow.body.resizeTextForBestFit = false; flow.body.raycastTarget = false;
            flow.body.horizontalOverflow = HorizontalWrapMode.Overflow; flow.body.verticalOverflow = VerticalWrapMode.Truncate;
            flow.heading.gameObject.SetActive(false); flow.nextButton.gameObject.SetActive(false);
            flow.backButton.gameObject.SetActive(false); flow.pageLabel.gameObject.SetActive(false);
        }
        public void Show(string value)
        {
            cues.Clear(); cue = 0; Canvas.ForceUpdateCanvases();
            var settings = body.GetGenerationSettings(Vector2.zero); settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = Mathf.Max(200, body.rectTransform.rect.width-8); string line = "";
            foreach(var word in value.Split(new[]{' ','\n','\r'},System.StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length==0?word:line+" "+word;
                float measured = body.cachedTextGeneratorForLayout.GetPreferredWidth(candidate,settings)/body.pixelsPerUnit;
                if(line.Length>0&&measured>width){cues.Add(line);line=word;}else line=candidate;
                if(line.EndsWith(".")||line.EndsWith("?")||line.EndsWith("!")){cues.Add(line);line="";}
            }
            if(line.Length>0)cues.Add(line); Begin(cues.Count>0?cues[0]:"");
        }
        void Begin(string value){text=value;visible=Application.isPlaying?0:text.Length;clock=0;Render();}
        public bool ConsumeNext()
        {
            if(IsTyping){visible=text.Length;Render();return true;}
            if(cue+1>=cues.Count)return false;
            Begin(cues[++cue]);return true;
        }
        void Render(){body.text=IsTyping?text.Substring(0,visible)+"<color=#FFFFFF00>"+text.Substring(visible)+"</color>":text;}
        public void Tick(float delta)
        {
            clock+=delta;bool changed=false;
            while(IsTyping){float wait=1/32f;if(visible>0&&".,!?;:".IndexOf(text[visible-1])>=0)wait+=.16f;if(clock<wait)break;clock-=wait;visible++;changed=true;}
            if(changed)Render();
        }
    }
}
