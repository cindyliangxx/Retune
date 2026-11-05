using UnityEngine;
namespace ReTune.Scenes
{
    public sealed class ReTunePartsArt : MonoBehaviour
    {
        public ReTuneSceneController controller;
        public Transform gear, small, windingKey, roller;
        public Collider2D rollerArea, windingArea;
        public float windingDuration=.7f, playbackDuration=5f;
        public MusicBoxController originalRoller;
        float winding, playing;
        Quaternion keyRest;
        SpriteRenderer defaultGear, defaultRoller;
        GearRotator gearDriver, smallDriver;
        bool gearDriverEnabled, smallDriverEnabled;
        Quaternion previousGearRotation;
        public bool IsBusy => winding>0 || playing>0;
        public bool IsPlaying => playing>0;
        void Awake()
        {
            keyRest=windingKey.localRotation;
            defaultGear=gear.GetComponent<SpriteRenderer>();
            defaultRoller=roller.GetComponent<SpriteRenderer>();
            if(originalRoller==null)originalRoller=roller.GetComponent<MusicBoxController>();
            gearDriver=gear.GetComponent<GearRotator>();smallDriver=small.GetComponent<GearRotator>();
            gearDriverEnabled=gearDriver!=null&&gearDriver.enabled;
            smallDriverEnabled=smallDriver!=null&&smallDriver.enabled;
            previousGearRotation=gear.rotation;
            SetSpeeds(false);
        }
        public void FitInstalledPart(ReTunePartDrag part,Collider2D slot)
        {
            var reference=slot==controller.structureSlot?gear:roller;
            // Fit the visible artwork, not the disabled whitebox sprite retained on the part.
            SpriteRenderer visual=null;
            foreach(var candidate in part.GetComponentsInChildren<SpriteRenderer>(true))
                if(candidate.enabled&&candidate.gameObject.activeInHierarchy&&candidate.sprite!=null)visual=candidate;
            var original=reference.GetComponent<SpriteRenderer>();
            if(visual==null || original==null || visual.sprite==null)return;
            float width=original.sprite.bounds.size.x*Mathf.Abs(reference.lossyScale.x);
            float current=visual.sprite.bounds.size.x*Mathf.Abs(visual.transform.lossyScale.x);
            float height=original.sprite.bounds.size.y*Mathf.Abs(reference.lossyScale.y);
            float currentHeight=visual.sprite.bounds.size.y*Mathf.Abs(visual.transform.lossyScale.y);
            if(current>0&&currentHeight>0)part.transform.localScale*=Mathf.Min(width/current,height/currentHeight);
        }
        public void RefreshInstalledParts()
        {
            if(defaultGear==null)defaultGear=gear.GetComponent<SpriteRenderer>();
            if(defaultRoller==null)defaultRoller=roller.GetComponent<SpriteRenderer>();
            defaultGear.enabled=ReTuneSession.structure<0;
            defaultRoller.enabled=ReTuneSession.tone<0;
            SetSpeeds(false);
        }
        public void CancelPlayback()
        {
            winding=playing=0;windingKey.localRotation=keyRest;
            SetSpeeds(false);controller.StopPartsAudio();
        }
        public void BeginWinding()
        {
            if(controller.CanInteract&&!IsBusy&&!AnyDragging())winding=Mathf.Max(.05f,windingDuration);
        }
        public bool BeginPreview()
        {
            if(IsBusy || AnyDragging())return false;
            playing=Mathf.Max(.1f,playbackDuration);SetSpeeds(false);return true;
        }
        bool AnyDragging(){foreach(var part in controller.partObjects)if(part!=null&&part.IsDragging)return true;return false;}
        void SetSpeeds(bool paused)
        {
            // Installed parts only run during playback, after winding has finished.
            bool running=playing>0&&!paused;
            if(gearDriver!=null)gearDriver.enabled=gearDriverEnabled&&running;
            if(smallDriver!=null)smallDriver.enabled=smallDriverEnabled&&running;
            float rollerSpeed=originalRoller!=null?originalRoller.baseSpeed:1;
            if(originalRoller!=null)originalRoller.SetRollerSpeed(ReTuneSession.tone<0&&running?rollerSpeed:0);
            if(controller.partObjects==null)return;
            foreach(var part in controller.partObjects)
            {
                if(part==null)continue;
                var animator=part.GetComponentInChildren<Animator>();if(animator==null)continue;
                bool installed=part.part==ReTuneSession.tone;
                animator.speed=paused||part.IsDragging||installed&&!running?0:rollerSpeed;
                var box=animator.GetComponent<MusicBoxController>();
                if(box!=null)box.tines=installed&&running&&!part.IsDragging&&originalRoller!=null?originalRoller.tines:System.Array.Empty<MusicBoxTineVisual>();
            }
        }
        void Update()
        {
            if(controller.PartsAnimationPaused){SetSpeeds(true);return;}
            bool pointer=ReTuneWorldPointer.TryPosition(controller.worldCamera,Input.mousePosition,0,out var point);
            if(controller.CanInteract&&windingArea!=null&&pointer&&windingArea.OverlapPoint(point)&&Input.GetMouseButtonDown(0))BeginWinding();
            float dt=Time.unscaledDeltaTime;
            if(winding>0)
            {
                winding=Mathf.Max(0,winding-dt);
                windingKey.localRotation=keyRest*Quaternion.Euler(360*(1-winding/Mathf.Max(.05f,windingDuration)),0,0);
                if(winding==0){windingKey.localRotation=keyRest;controller.Trial();}
            }
            if(playing>0)
            {
                playing=Mathf.Max(0,playing-dt);
                if(playing==0)controller.StopPartsAudio();
            }
            SetSpeeds(false);
        }
        void LateUpdate()
        {
            // The authored GearRotator remains the slot's driver even when its sprite is hidden.
            // Copy its actual delta, including direction, speed changes and stutter effects.
            Quaternion delta=gear.rotation*Quaternion.Inverse(previousGearRotation);
            previousGearRotation=gear.rotation;
            if(controller.PartsAnimationPaused||playing<=0)return;
            foreach(var part in controller.partObjects)
                if(part!=null&&part.part==ReTuneSession.structure&&!part.IsDragging)
                    part.transform.rotation=delta*part.transform.rotation;
        }
        void OnDisable(){if(windingKey!=null)windingKey.localRotation=keyRest;SetSpeeds(true);}
    }
}
