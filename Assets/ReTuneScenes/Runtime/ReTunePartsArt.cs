using UnityEngine;
namespace ReTune.Scenes
{
    public sealed class ReTunePartsArt : MonoBehaviour
    {
        public ReTuneSceneController controller;
        public Transform gear, small, windingKey, roller;
        public Collider2D rollerArea, windingArea;
        public float windingDuration = .7f, playbackDuration = 5f;
        float winding, playing;
        bool hovering;
        Vector3 rollerScale;
        Quaternion keyRest;
        void Awake(){ rollerScale=roller.localScale; keyRest=windingKey.localRotation; }
        public void BeginWinding()
        {
            if (controller != null && controller.CanInteract && winding <= 0 && playing <= 0) winding = Mathf.Max(.05f, windingDuration);
        }
        void Update()
        {
            if (controller.PartsAnimationPaused) { hovering=false; return; }
            bool pointer = ReTuneWorldPointer.TryPosition(controller.worldCamera,Input.mousePosition,0,out var point);
            hovering=controller.CanInteract && pointer && rollerArea.OverlapPoint(point);
            if(controller.CanInteract && windingArea != null && pointer && windingArea.OverlapPoint(point) && Input.GetMouseButtonDown(0) && winding<=0 && playing<=0)
                winding=windingDuration;
            if(winding>0)
            {
                winding=Mathf.Max(0,winding-Time.deltaTime);
                windingKey.localRotation=keyRest*Quaternion.Euler(360*(1-winding/windingDuration),0,0);
                if(winding==0){ windingKey.localRotation=keyRest;playing=playbackDuration;controller.Trial(); }
            }
            if(playing>0)
            {
                playing=Mathf.Max(0,playing-Time.deltaTime);
                gear.Rotate(0,0,-65*Time.deltaTime); small.Rotate(0,0,130*Time.deltaTime);
                foreach(var part in controller.partObjects)
                    if(part.part==ReTuneSession.structure) foreach(var sprite in part.GetComponentsInChildren<SpriteRenderer>())
                        if(sprite.enabled)sprite.transform.Rotate(0,0,-65*Time.deltaTime);
            }
            // A horizontal cylinder turns around its axle, rather than spinning flat like a gear.
            bool turning=hovering || playing>0;
            roller.localScale=Vector3.Scale(rollerScale,new Vector3(1,turning?.93f+.07f*Mathf.Cos(Time.time*18):1,1));
        }
        void OnDisable(){if(roller!=null)roller.localScale=rollerScale; if(windingKey!=null)windingKey.localRotation=keyRest;}
    }
}



