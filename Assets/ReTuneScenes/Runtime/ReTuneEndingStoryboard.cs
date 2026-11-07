using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    [ExecuteAlways]
    public sealed class ReTuneEndingStoryboard : MonoBehaviour
    {
        public ReTuneNarrativePlayer player;
        public RawImage figure;
        public Texture2D restingFigure;
        public Texture2D[] letterFrames;
        public GameObject envelope;
        [Header("Envelope close-up: replace Source Image on this Image")]
        public Image envelopeImage;
        public GameObject envelopePlaceholder;
        public float envelopeZoomDuration=1.4f;
        CanvasGroup envelopeOpacity;
        public Text intertitle;
        public Image shade;
        public GameObject postman;
        public RectTransform artwork;
        public Animator figureAnimator;
        public float animationDuration = 4.9f;
        public float departureDuration = 2.65f;
        ReTuneCinematicSubtitle subtitles;
        int page=-1;
        float age, clipAge;
        public bool IsAnimating => page<2 && clipAge<animationDuration || page==2 && age<envelopeZoomDuration || page==3 && age<departureDuration;
        void OnEnable(){ConfigurePresentation();}
        public void ConfigurePresentation()
        {
            if(player==null)player=GetComponent<ReTuneNarrativePlayer>();
            if(player==null||player.panel==null)return;
            ReTuneCinematicSubtitle.Layout(player);
            if(intertitle!=null){intertitle.font=player.body.font;intertitle.fontSize=44;intertitle.color=ReTuneUITheme.Ink;}
            FitArtwork();
        }
        public void Show(int index)
        {
            int previous=page;bool start=page<0;page=index;age=0;ConfigurePresentation();
            figure.gameObject.SetActive(true);
            if(envelope!=null){envelope.SetActive(index==2);if(index==2){
                envelope.transform.localScale=Vector3.one*.72f;
                envelopeOpacity=envelope.GetComponent<CanvasGroup>();if(envelopeOpacity==null)envelopeOpacity=envelope.AddComponent<CanvasGroup>();envelopeOpacity.alpha=0;
                bool supplied=envelopeImage!=null&&envelopeImage.sprite!=null;
                if(envelopeImage!=null){envelopeImage.preserveAspect=true;envelopeImage.color=supplied?Color.white:new Color(.91f,.86f,.72f,1);}
                if(envelopePlaceholder!=null)envelopePlaceholder.SetActive(!supplied);
            }}
            if(postman!=null)postman.SetActive(false);
            if(start){clipAge=0;if(figureAnimator!=null){figureAnimator.enabled=true;figureAnimator.Play("Post Letter",0,0);}}
            if(index==2&&figureAnimator!=null)figureAnimator.speed=0;
            if(index==3&&previous!=3&&figureAnimator!=null){figureAnimator.speed=1;figureAnimator.Play("Walk Away",0,0);}
            bool credits=index>=4;
            intertitle.gameObject.SetActive(credits);
            intertitle.text="Re:Tune\nThank you for listening.\n\nThe end.";
            player.panel.SetActive(!credits);
            // The establishing composition is visible immediately on scene entry.
            shade.color=new Color(.08f,.055f,.035f,credits?.94f:index==2?.65f:0);
            if(!credits){if(subtitles==null)subtitles=new ReTuneCinematicSubtitle(player.body);subtitles.Show(player.body.text);}
        }
        public bool ConsumeNext()
        {
            if(page<4&&subtitles!=null&&subtitles.ConsumeNext())return true;
            return IsAnimating;
        }
        void Update()
        {
            if(!Application.isPlaying||page<0)return;
            bool paused=ReTuneExitDialog.IsOpen||player.controller!=null&&player.controller.PartsAnimationPaused;
            if(figureAnimator!=null)figureAnimator.speed=paused||page==2||page>=4?0:1;
            if(paused)return;
            float dt=Time.unscaledDeltaTime;age+=dt;if(page<2)clipAge+=dt;
            if(page<4)subtitles?.Tick(dt);
            if(page==2&&envelope!=null){float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/envelopeZoomDuration));envelope.transform.localScale=Vector3.one*Mathf.Lerp(.72f,1.1f,t);if(envelopeOpacity!=null)envelopeOpacity.alpha=Mathf.Clamp01(age/.4f);}
            // Keep the street visible during the separate departure shot.
            if(page>=4){var ink=ReTuneUITheme.Ink;ink.a=Mathf.Clamp01(age/1);intertitle.color=ink;}
        }
        void LateUpdate(){FitArtwork();}
        void FitArtwork(){if(artwork!=null&&artwork.parent is RectTransform parent)artwork.localScale=Vector3.one*Mathf.Max(parent.rect.width/1280,parent.rect.height/720);}
    }
}
