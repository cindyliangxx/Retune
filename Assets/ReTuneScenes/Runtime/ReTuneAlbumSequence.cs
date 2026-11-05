using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    public sealed class ReTuneAlbumSequence : MonoBehaviour
    {
        public Sprite albumSpread;
        [Header("相册独立图层（同画布原位置）")]
        public Sprite[] photoLayers;
        public Sprite[] photoCloseups;
        public Sprite closedAlbumCover;
        [Min(.1f)] public float flipDuration=.55f;
        public bool IsOpen { get { return root!=null&&root.activeSelf; } }
        public bool BlocksDialogueInput { get { return IsOpen||Time.frameCount==resumeFrame; } }
        public bool IsViewingPhoto { get { return zoom!=null&&zoom.activeSelf; } }
        GameObject root,closed,spread,zoom;
        RectTransform coverRect,spreadRect,photoRect,photoViewport;
        CanvasGroup coverFade,spreadFade;
        ReTuneNarrativePlayer player;
        bool portraitWasActive,panelWasActive,flipping;
        float flipAge;
        int resumeFrame=-1;
        Font font;
        public void Open(ReTuneNarrativePlayer owner)
        {
            player=owner;if(root==null)Build();
            panelWasActive=player.panel.activeSelf;portraitWasActive=player.visitorPortraits!=null&&player.visitorPortraits.activeSelf;
            player.panel.SetActive(false);if(player.visitorPortraits!=null)player.visitorPortraits.SetActive(false);
            root.SetActive(true);root.transform.SetAsLastSibling();closed.SetActive(true);spread.SetActive(false);zoom.SetActive(false);
            flipping=false;coverFade.alpha=1;coverRect.localScale=Vector3.one;spreadRect.localScale=Vector3.one;
        }
        public void OpenAlbum()
        {
            if(!IsOpen||flipping||!closed.activeSelf)return;
            flipping=true;flipAge=0;spread.SetActive(true);spreadFade.alpha=0;
        }
        void Update()
        {
            if(!flipping||player.controller!=null&&player.controller.PartsAnimationPaused)return;
            flipAge+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(flipAge/Mathf.Max(.1f,flipDuration));float eased=t*t*(3-2*t);
            coverRect.localScale=new Vector3(Mathf.Max(.02f,1-eased),1,1);coverFade.alpha=1-eased;
            spreadRect.localScale=new Vector3(Mathf.Lerp(.65f,1,eased),1,1);spreadFade.alpha=eased;
            if(t>=1){flipping=false;closed.SetActive(false);spreadRect.localScale=Vector3.one;}
        }
        public void ViewPhoto(int index)
        {
            if(!IsOpen||flipping||closed.activeSelf||index<0||index>1)return;
            zoom.SetActive(true);Canvas.ForceUpdateCanvases();
            if(photoCloseups!=null&&index<photoCloseups.Length&&photoCloseups[index]!=null)
            {
                var image=photoRect.GetComponent<Image>();image.sprite=photoCloseups[index];image.preserveAspect=true;image.alphaHitTestMinimumThreshold=.1f;
                float photoRatio=image.sprite.rect.width/image.sprite.rect.height;
                float photoWidth=Mathf.Min(photoViewport.rect.width-32,(photoViewport.rect.height-32)*photoRatio);
                photoRect.sizeDelta=new Vector2(photoWidth,photoWidth/photoRatio);photoRect.anchoredPosition=Vector2.zero;
                return;
            }
            float ratio=albumSpread.rect.width/albumSpread.rect.height;
            float width=Mathf.Min(photoViewport.rect.width,photoViewport.rect.height*ratio);
            float magnification=index==0?1.8f:2.6f;
            var size=new Vector2(width,width/ratio)*magnification;
            var focus=index==0?new Vector2(.25f,.41f):new Vector2(.724f,.532f);
            photoRect.sizeDelta=size;photoRect.anchoredPosition=Vector2.Scale(Vector2.one*.5f-focus,size);
        }
        public void BackToAlbum(){if(zoom!=null)zoom.SetActive(false);}
        public void ContinueDialogue()
        {
            if(!IsOpen||flipping)return;
            root.SetActive(false);player.panel.SetActive(panelWasActive);
            if(player.visitorPortraits!=null)player.visitorPortraits.SetActive(portraitWasActive);
            player.Next();
            resumeFrame=Time.frameCount;
        }
        void Build()
        {
            font=player.body.font;
            var parent=player.panel.GetComponentInParent<Canvas>().transform;
            var rootRect=Rect("Interactive Album",parent,Vector2.zero);Stretch(rootRect);root=rootRect.gameObject;
            var dim=root.AddComponent<Image>();dim.color=new Color(.12f,.09f,.07f,.68f);dim.raycastTarget=true;
            closed=Rect("Closed Album",rootRect,new Vector2(340,430)).gameObject;
            coverRect=(RectTransform)closed.transform;coverRect.anchoredPosition=new Vector2(0,-20);
            var cover=closed.AddComponent<Image>();cover.color=new Color(.32f,.27f,.30f);coverFade=closed.AddComponent<CanvasGroup>();
            if(closedAlbumCover!=null){cover.sprite=closedAlbumCover;cover.color=Color.white;cover.preserveAspect=true;}
            var open=closed.AddComponent<Button>();open.targetGraphic=cover;open.onClick.AddListener(OpenAlbum);
            // A replaceable plain cover until a closed-book illustration is supplied.
            if(closedAlbumCover==null)Label("Memories",coverRect,48,new Vector2(0,35),new Vector2(290,80));
            Label("Click to open",coverRect,26,new Vector2(0,-120),new Vector2(290,50));
            var spine=Rect("Spine",coverRect,new Vector2(12,410));spine.anchoredPosition=new Vector2(-150,0);spine.gameObject.AddComponent<Image>().color=new Color(.65f,.53f,.43f);
            spreadRect=Rect("Open Album",rootRect,new Vector2(1080,610));spread=spreadRect.gameObject;
            var art=spread.AddComponent<Image>();art.sprite=albumSpread;art.preserveAspect=true;art.raycastTarget=false;spreadFade=spread.AddComponent<CanvasGroup>();
            // Hotspots are attached to an image-sized frame, not its surrounding margins.
            float ratio=albumSpread.rect.width/albumSpread.rect.height;spreadRect.sizeDelta=new Vector2(1080,1080/ratio);
            if(photoLayers!=null&&photoLayers.Length==2&&photoLayers[0]!=null&&photoLayers[1]!=null)
            {
                for(int i=0;i<2;i++)
                {
                    int index=i;var layer=Rect(i==0?"Left Photo Layer":"Right Photo Layer",spreadRect,Vector2.zero);Stretch(layer);
                    var image=layer.gameObject.AddComponent<Image>();image.sprite=photoLayers[i];image.preserveAspect=false;image.alphaHitTestMinimumThreshold=.1f;
                    var button=layer.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.targetGraphic=image;button.onClick.AddListener(()=>ViewPhoto(index));
                }
            }
            else
            {
                Hotspot("Left Photo",spreadRect,new Vector2(.10f,.12f),new Vector2(.40f,.72f),()=>ViewPhoto(0));
                Hotspot("Right Photo",spreadRect,new Vector2(.545f,.35f),new Vector2(.90f,.72f),()=>ViewPhoto(1));
            }
            var next=TextButton("Continue",spreadRect,new Vector2(430,-spreadRect.sizeDelta.y/2-35),new Vector2(220,55),ContinueDialogue);
            Label("Click a photo to look closer",spreadRect,26,new Vector2(-250,-spreadRect.sizeDelta.y/2-35),new Vector2(560,55));
            zoom=Rect("Photo Closeup",rootRect,Vector2.zero).gameObject;Stretch((RectTransform)zoom.transform);
            var back=zoom.AddComponent<Image>();back.color=new Color(.08f,.06f,.05f,.92f);var returnButton=zoom.AddComponent<Button>();returnButton.targetGraphic=back;returnButton.transition=Selectable.Transition.None;returnButton.onClick.AddListener(BackToAlbum);
            photoViewport=Rect("Photo Viewport",zoom.transform,new Vector2(840,510));photoViewport.gameObject.AddComponent<Image>().color=new Color(.25f,.20f,.17f);photoViewport.gameObject.AddComponent<RectMask2D>();
            photoRect=Rect("Magnified Album Photo",photoViewport,Vector2.zero);var photo=photoRect.gameObject.AddComponent<Image>();photo.sprite=albumSpread;photo.preserveAspect=false;photo.raycastTarget=true;
            // Absorb clicks within the photo; only its exterior closes the closeup.
            var absorb=photoRect.gameObject.AddComponent<Button>();absorb.transition=Selectable.Transition.None;
            TextButton("Back to album",zoom.transform,new Vector2(0,-300),new Vector2(360,55),BackToAlbum);
            root.SetActive(false);
        }
        static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;return rect;
        }
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        Text Label(string text,Transform parent,int size,Vector2 position,Vector2 bounds)
        {
            var rect=Rect(text,parent,bounds);rect.anchoredPosition=position;var label=rect.gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;label.color=new Color(1,.95f,.81f);label.alignment=TextAnchor.MiddleCenter;label.text=text;label.raycastTarget=false;return label;
        }
        Button TextButton(string text,Transform parent,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(text,parent,size);rect.anchoredPosition=position;var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.27f,.21f,.16f,.9f);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);Label(text,rect,30,Vector2.zero,size);return button;
        }
        static void Hotspot(string name,Transform parent,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(name,parent,Vector2.zero);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=rect.gameObject.AddComponent<Image>();image.color=Color.clear;var button=rect.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.targetGraphic=image;button.onClick.AddListener(action);
        }
    }
}
