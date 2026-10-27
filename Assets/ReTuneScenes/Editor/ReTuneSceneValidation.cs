using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;

[InitializeOnLoad]
public static class ReTuneSceneValidation
{
    const string Running = "ReTuneSceneFlowValidation";
    static double next;
    static ReTuneSceneValidation() { EditorApplication.update += Tick; }
    public static void Run()
    {
        try
        {
            ReTuneSceneBuilder.Generate();
            foreach (ReTuneSceneController.Screen screen in Enum.GetValues(typeof(ReTuneSceneController.Screen)))
            {
                EditorSceneManager.OpenScene(ReTuneSceneController.ScenePath(screen));
                var c = UnityEngine.Object.FindObjectOfType<ReTuneSceneController>();
                Check(c != null && c.screen == screen && c.story != null, "Saved scene entry: " + screen);
                Check(c.worldCamera != null && c.worldCamera.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() != null, "World pointer camera");
                Check(UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true).Length > 0, "Authored world sprites");
                Check(UnityEngine.Object.FindObjectsOfType<Text>(true).Length > 5, "Text exists before Play: " + screen);
                foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>(true))
                {
                    Check(button.onClick.GetPersistentEventCount() == 1, "Saved button action: " + button.name);
                    Check(button.onClick.GetPersistentTarget(0) == c, "Saved button controller: " + button.name);
                }
                Check(c.overlay != null && !c.overlay.activeSelf, "Pre-existing hidden overlay");
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<Transform>(true))
                    for (int i=0;i<obj.GetComponents<Component>().Length;i++) Check(obj.GetComponents<Component>()[i] != null, "No missing scripts: " + obj.name);
                if (screen == ReTuneSceneController.Screen.Parts)
                {
                    var parts = UnityEngine.Object.FindObjectsOfType<ReTunePartDrag>();
                    Check(parts.Length == 6 && parts.All(part => part.GetComponent<RectTransform>() == null && part.GetComponentInChildren<Collider2D>() != null), "Six authored world parts");
                }
                Capture(screen.ToString());
            }
            EditorSceneManager.OpenScene(ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Menu));
            SessionState.SetBool(Running,true); SessionState.SetInt("ReTuneFlowStep",0);
            SessionState.SetFloat("ReTuneFlowStarted",(float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Running,false)) return;
        try
        {
            if(EditorApplication.timeSinceStartup - SessionState.GetFloat("ReTuneFlowStarted",0)>100) throw new Exception("Flow validation timed out");
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling) return;
            var c=UnityEngine.Object.FindObjectOfType<ReTuneSceneController>(); if(c==null) return;
            int step=SessionState.GetInt("ReTuneFlowStep",0);
            switch(step)
            {
                case 0:
                    Check(c.screen==ReTuneSceneController.Screen.Menu,"Start scene"); c.StartHealing(); break;
                case 1:
                    Check(c.screen==ReTuneSceneController.Screen.Shop,"Shop scene");
                    for(int i=0;i<4;i++) c.ContinueStory(); break;
                case 2:
                    Check(c.screen==ReTuneSceneController.Screen.Parts,"Parts scene");
                    int index=ReTuneSession.customer;
                    c.InstallPart((index+1)%4); c.InstallPart(4+(index+1)%2); Check(c.RecipeMatches()==0,"Wrong recipe");
                    c.InstallPart(index); Check(c.RecipeMatches()==1,"Partial recipe");
                    c.ClearParts(); Check(ReTuneSession.structure<0 && ReTuneSession.tone<0,"Empty slots");
                    c.InstallPart(index); c.InstallPart(4+index%2); Check(c.RecipeMatches()==2,"Correct recipe"); c.Trial();
                    next=EditorApplication.timeSinceStartup+2.5; break;
                case 3:
                    Check(c.screen==ReTuneSceneController.Screen.Performance,"Automatic parts-to-performance scene load");
                    ReTuneSession.taughtClick=ReTuneSession.taughtHold=true;
                    // Insufficient stability must reset the current scene, not load the next one.
                    Set(c,"note",8); Set(c,"time",24f); Set(c,"stableTime",0f); Call(c,"TickPerformance",.01f);
                    Check(c.CurrentNote==0,"Low stability reset");
                    // Test the same public entry points used by the saved pointer handlers.
                    int holdIndex=ReTuneSession.customer==2?4:3;
                    Set(c,"note",holdIndex); Set(c,"time",c.firstNoteTime+holdIndex*c.noteSpacing); c.NoteDown(); c.NoteRelease();
                    Check(c.CurrentNote==holdIndex+1,"Early-release miss advances note");
                    c.ResetPerformance();
                    Set(c,"time",c.firstNoteTime); c.NoteDown(); Check(c.CurrentNote==1,"Timed mouse click");
                    Set(c,"note",holdIndex); Set(c,"time",c.firstNoteTime+holdIndex*c.noteSpacing); c.NoteDown(); Call(c,"TickPerformance",c.holdDuration+.01f);
                    Check(c.CurrentNote==holdIndex+1,"Hold completion");
                    while(c.CurrentNote<c.noteCount) Call(c,"ResolveNote",true);
                    Set(c,"time",24f); Set(c,"stableTime",20f); Call(c,"TickPerformance",.01f); break;
                case 4:
                    Check(c.screen==ReTuneSceneController.Screen.Reply,"Reply scene");
                    Check(ReTuneSession.hits>0 && ReTuneSession.structure==ReTuneSession.customer,"Session survives scene load");
                    bool last=ReTuneSession.customer==3;
                    c.ContinueStory();
                    if(!last) { SessionState.SetInt("ReTuneFlowStep",1); next=EditorApplication.timeSinceStartup+.5; return; }
                    break;
                case 5:
                    Check(c.screen==ReTuneSceneController.Screen.Ending,"Ending scene"); c.MainMenu(); break;
                case 6:
                    Check(c.screen==ReTuneSceneController.Screen.Menu && !ReTuneSession.active,"Return to menu resets session");
                    Debug.Log("RETUNE_SCENE_FLOW_PASS: six saved layouts, persistent buttons, four customer scene loops, recipe checks, click/hold/release, stability restart, ending and reset");
                    SessionState.SetBool(Running,false); EditorApplication.Exit(0); return;
            }
            SessionState.SetInt("ReTuneFlowStep",step+1);
            if(step!=2) next=EditorApplication.timeSinceStartup+.5;
        }
        catch(Exception e) { SessionState.SetBool(Running,false); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Capture(string name)
    {
        var cam=UnityEngine.Object.FindObjectOfType<Camera>();
        var canvas=cam.gameObject.scene.GetRootGameObjects().Select(go => go.GetComponent<Canvas>()).First(value => value != null);
        var rt=new RenderTexture(1280,720,24); rt.Create(); cam.targetTexture=rt;
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=cam; canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases(); cam.Render(); RenderTexture.active=rt;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        string folder="ValidationPreviews"; Directory.CreateDirectory(folder); File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=null; cam.targetTexture=null; canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null;
        UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }
    static void Check(bool ok,string name) { if(!ok) throw new Exception("ReTune scene validation failed: "+name); }
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static void Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
}
