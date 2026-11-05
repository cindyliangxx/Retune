using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    public sealed class ReTuneBackgroundMusic : MonoBehaviour
    {
        const string VolumeKey="ReTune.BGM.Volume";
        static ReTuneBackgroundMusic instance;
        AudioSource source;
        RectTransform volumePanel;
        Text volumeValue;
        bool started,hasPlayback,paused,blocked,volumeDirty;
        float volume=.4f;
        public static ReTuneBackgroundMusic Instance { get { return instance; } }
        public float Volume { get { return volume; } }
        public bool IsBlocked { get { return blocked; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap(){Ensure();}
        static ReTuneBackgroundMusic Ensure()
        {
            if(instance==null)new GameObject("ReTune Background Music").AddComponent<ReTuneBackgroundMusic>();
            return instance;
        }
        void Awake()
        {
            if(instance!=null&&instance!=this){Destroy(gameObject);return;}
            instance=this;DontDestroyOnLoad(gameObject);
            volume=Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,.4f));
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=0;
            source.clip=Resources.Load<AudioClip>("Audio/Background/LoFiBGM");
            if(source.clip==null)Debug.LogError("ReTune: missing LoFi background music.");
            BuildVolumeUI();SceneManager.sceneLoaded+=SceneLoaded;
        }
        public static void StartOpeningMusic()
        {
            if(!Application.isPlaying)return;
            var music=Ensure();music.started=true;music.ApplyScene(SceneManager.GetActiveScene().name);
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(mode!=LoadSceneMode.Single)return;
            if(scene.name=="00_Opening"){source.Stop();hasPlayback=false;started=false;paused=false;source.volume=0;}
            ApplyScene(scene.name);
        }
        void ApplyScene(string name)
        {
            blocked=name=="02_Parts"||name=="03_Performance";
            if(!blocked&&(name=="00_Start"||name=="01_Shop"||name=="04_Reply"||name=="05_Ending"))started=true;
            if(blocked)
            {
                if(hasPlayback&&!paused)source.Pause();paused=true;source.volume=0;return;
            }
            if(!started||source.clip==null)return;
            if(hasPlayback){if(paused)source.UnPause();}
            else{source.Play();hasPlayback=true;}
            paused=false;
        }
        public void SetVolume(float value)
        {
            volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat(VolumeKey,volume);volumeDirty=true;
            if(volumeValue!=null)volumeValue.text=Mathf.RoundToInt(volume*100)+"%";
        }
        public static bool IsPointerOverVolumeControl()
        {
            return instance!=null&&instance.volumePanel!=null&&RectTransformUtility.RectangleContainsScreenPoint(instance.volumePanel,Input.mousePosition,null);
        }
        void Update()
        {
            if(source!=null)source.volume=Mathf.MoveTowards(source.volume,started&&!blocked?volume:0,Time.unscaledDeltaTime/.75f);
            if(Input.GetMouseButtonUp(0))SaveVolume();
        }
        void SaveVolume(){if(volumeDirty){PlayerPrefs.Save();volumeDirty=false;}}
        void OnApplicationFocus(bool focus){if(!focus)SaveVolume();}
        void OnApplicationQuit(){SaveVolume();}
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;if(instance==this)instance=null;}
        void BuildVolumeUI()
        {
            var root=new GameObject("Music Volume UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.layer=5;root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            volumePanel=Rect("BGM Volume",root.transform,new Vector2(300,58));volumePanel.anchorMin=volumePanel.anchorMax=volumePanel.pivot=Vector2.one;volumePanel.anchoredPosition=new Vector2(-18,-16);
            var background=volumePanel.gameObject.AddComponent<Image>();background.color=ReTuneUITheme.Surface;volumePanel.gameObject.AddComponent<ReTuneSubtitleGradient>();
            Label("BGM",volumePanel,new Vector2(44,0),new Vector2(64,44));
            volumeValue=Label(Mathf.RoundToInt(volume*100)+"%",volumePanel,new Vector2(266,0),new Vector2(62,44));
            var sliderRect=Rect("BGM Slider",volumePanel,new Vector2(154,36));sliderRect.anchorMin=sliderRect.anchorMax=new Vector2(0,.5f);sliderRect.anchoredPosition=new Vector2(156,0);
            var hitArea=sliderRect.gameObject.AddComponent<Image>();hitArea.color=Color.clear;
            var slider=sliderRect.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.direction=Slider.Direction.LeftToRight;
            var track=Rect("Track",sliderRect,new Vector2(142,4));track.gameObject.AddComponent<Image>().color=new Color(.85f,.75f,.56f,.5f);
            var fillArea=Rect("Fill Area",sliderRect,new Vector2(142,4));
            var fill=Rect("Fill",fillArea,Vector2.zero);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;fill.gameObject.AddComponent<Image>().color=new Color(1,.8f,.42f);
            var handleArea=Rect("Handle Area",sliderRect,new Vector2(142,36));var handle=Rect("Handle",handleArea,new Vector2(14,20));var handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=new Color(1,.96f,.84f);
            slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handleImage;slider.SetValueWithoutNotify(volume);slider.onValueChanged.AddListener(SetVolume);
            var navigation=slider.navigation;navigation.mode=Navigation.Mode.None;slider.navigation=navigation;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;return rect;
        }
        static Text Label(string value,Transform parent,Vector2 position,Vector2 size)
        {
            var rect=Rect(value,parent,size);rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.anchoredPosition=position;
            var text=rect.gameObject.AddComponent<Text>();text.font=Resources.Load<Font>("Fonts/LeMiShiguang");text.fontSize=26;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.96f,.84f);text.raycastTarget=false;text.text=value;return text;
        }
    }
}
