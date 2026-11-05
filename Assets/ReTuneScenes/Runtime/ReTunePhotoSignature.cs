using UnityEngine;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    [RequireComponent(typeof(RawImage))]
    public sealed class ReTunePhotoSignature : MonoBehaviour
    {
        public ReTuneSceneController controller;
        public Texture2D[] frames;
        public Texture2D finalFrame;
        [Min(.1f)] public float duration=1.5f;
        [Min(0)] public float delay=.2f;
        // All supplied frames share one canvas. Use the same crop throughout the animation.
        public Rect sourceRect=new Rect(509f/1920f,241f/1080f,722f/1920f,515f/1080f);
        RawImage image;
        float age;
        int shown=-1;
        public bool IsWriting => age<delay+duration;
        public float Aspect => finalFrame!=null?finalFrame.width*sourceRect.width/(finalFrame.height*sourceRect.height):722f/515f;
        public void Replay(Texture2D complete,Texture2D[] sequence,ReTuneSceneController owner)
        {
            controller=owner;frames=sequence;finalFrame=complete;age=0;shown=-1;
            image=GetComponent<RawImage>();image.material=null;image.uvRect=sourceRect;
            image.texture=null;image.raycastTarget=false;image.color=Color.white;image.enabled=false;
        }
        void Update()
        {
            if(controller!=null&&controller.PartsAnimationPaused)return;
            age+=Time.unscaledDeltaTime;
            if(image==null||frames==null||frames.Length==0||age<delay)return;
            image.enabled=true;
            if(!IsWriting){image.texture=finalFrame!=null?finalFrame:frames[frames.Length-1];return;}
            int index=Mathf.Clamp(Mathf.FloorToInt((age-delay)/Mathf.Max(.1f,duration)*frames.Length),0,frames.Length-1);
            if(index!=shown){shown=index;image.texture=frames[index];}
        }
    }
}
