using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ReTune.Scenes;

[InitializeOnLoad]
public static class ReTunePartsSmoke
{
    static int stage;
    static double next, deadline;
    static Vector3[] homes, scales;
    static Quaternion rotation;
    static Sprite frame;
    static ReTuneSceneController c;
    static ReTunePartsArt art;
    static ReTunePartsSmoke(){EditorApplication.playModeStateChanged+=OnState;}
    public static void Run()
    {
        ReTunePartsUpgrade.Configure();
        SessionState.SetBool("PartsUpgradeSmoke",true);
        EditorApplication.isPlaying=true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PartsUpgradeSmoke",false))
        {stage=0;deadline=EditorApplication.timeSinceStartup+55;next=EditorApplication.timeSinceStartup+.8;EditorApplication.update+=Tick;}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static ReTunePartDrag Part(int id)=>c.partObjects.Single(x=>x.part==id);
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup>deadline){Finish(false,"Timeout at stage "+stage);return;}
        if(EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.4;
        try
        {
            if(stage==0)
            {
                c=UnityEngine.Object.FindObjectOfType<ReTuneSceneController>();art=c.GetComponent<ReTunePartsArt>();c.CloseOverlay();
                homes=c.partObjects.Select(p=>p.transform.localPosition).ToArray();scales=c.partObjects.Select(p=>p.transform.localScale).ToArray();
                Check(!c.trackA.isPlaying,"Parts should start silent");
                frame=Part(4).GetComponentInChildren<SpriteRenderer>().sprite;
            }
            else if(stage==1)
            {
                Check(Part(4).GetComponentInChildren<Animator>().speed>0,"Shelf rollers should loop");
                // Two structurally distinct clips, not the same frame sequence reused.
                Check(Part(4).GetComponentInChildren<Animator>().runtimeAnimatorController!=Part(5).GetComponentInChildren<Animator>().runtimeAnimatorController,"Rollers need different groups");
                c.InstallPart(0);c.InstallPart(4);
                Check(!art.gear.GetComponent<SpriteRenderer>().enabled&&!art.roller.GetComponent<SpriteRenderer>().enabled,"Original parts must be concealed");
                Check(Vector3.Distance(Part(4).GetComponentInChildren<SpriteRenderer>().bounds.center,c.toneSlot.bounds.center)<.01f,"Roller alignment");
                Check(Mathf.Abs(Part(4).GetComponentInChildren<SpriteRenderer>().bounds.size.x-art.roller.GetComponent<SpriteRenderer>().bounds.size.x)<.01f,"Roller fitted size");
                c.InstallPart(1);c.InstallPart(5);
                Check(ReTuneSession.structure==1&&ReTuneSession.tone==5,"Replacement selection");
                for(int i=0;i<c.partObjects.Length;i++)if(c.partObjects[i].part==0||c.partObjects[i].part==4)
                {Check(Vector3.Distance(c.partObjects[i].transform.localPosition,homes[i])<.01f,"Replaced part must return home");Check(Vector3.Distance(c.partObjects[i].transform.localScale,scales[i])<.01f,"Replaced part must recover scale");}
                c.ClearParts();Check(art.gear.GetComponent<SpriteRenderer>().enabled&&art.roller.GetComponent<SpriteRenderer>().enabled,"Original parts should return");
                c.InstallPart(0);c.InstallPart(4);
                // Dropping a rhythm part over the tone slot must preserve the current recipe.
                c.DropPart(1,c.worldCamera.WorldToScreenPoint(c.toneSlot.bounds.center),c.worldCamera);
                Check(ReTuneSession.structure==0&&ReTuneSession.tone==4,"Wrong slot must not install");
                c.InstallPart(1);Check(!art.IsPlaying,"Installation must not require an audio preview");

                rotation=Part(1).transform.localRotation;
            }
            else if(stage==2)
            {
                Check(Quaternion.Angle(rotation,Part(1).transform.localRotation)<.01f,"Installed gear must stay still before winding");
                Check(Part(4).GetComponentInChildren<Animator>().speed==0,"Installed roller must stay still before winding");
                c.Trial();Check(c.CanInteract&&art.IsPlaying,"Wrong recipe should stay in selection and preview");
                Check(Mathf.Approximately(c.trackB.volume,.2f),"Partial recipe should blend half the response track");
                c.Pause();rotation=Part(1).transform.localRotation;
            }
            else if(stage==3)
            {
                Check(Quaternion.Angle(rotation,Part(1).transform.localRotation)<.01f,"Pause must stop gear");
                Check(Part(4).GetComponentInChildren<Animator>().speed==0&&Part(5).GetComponentInChildren<Animator>().speed==0,"Pause must stop roller loops");
                c.CloseOverlay();c.ClearParts();c.OriginalMelody();
                Check(art.IsPlaying&&c.trackB.volume==0,"Original preview excludes response track");
            }
            else if(stage==4)
            {
                c.ClearParts();c.InstallPart(0);c.InstallPart(4);c.Trial();
                Check(!c.CanInteract&&art.IsPlaying,"Correct recipe should lock transition and preview");
                Check(Mathf.Approximately(c.trackB.volume,.4f)&&c.trackC.volume==0,"Correct recipe response and noise mix");
                next=EditorApplication.timeSinceStartup+2.2;
            }
            else if(stage==5)
            {Check(SceneManager.GetActiveScene().name=="02_Parts","Must not cut off five-second preview early");next=EditorApplication.timeSinceStartup+3.5;}
            else if(stage==6)
            {Check(SceneManager.GetActiveScene().name=="03_Performance","Correct recipe should reach performance");Finish(true,"replacement, alignment, rotation, pause, preview and transition");return;}
            stage++;
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool ok,string detail)
    {
        SessionState.SetBool("PartsUpgradeSmoke",false);EditorApplication.update-=Tick;
        Debug.Log((ok?"PARTS_UPGRADE_SMOKE_PASS ":"PARTS_UPGRADE_SMOKE_FAIL ")+detail);
        EditorApplication.Exit(ok?0:1);
    }
}
