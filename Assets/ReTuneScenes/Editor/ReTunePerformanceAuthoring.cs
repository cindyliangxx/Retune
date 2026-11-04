using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTunePerformanceAuthoring
{
    const string Path="Assets/ReTuneScenes/Scenes/03_Performance.unity";
    static Material material;
    static ReTunePerformanceAuthoring() { EditorApplication.delayCall += AutoApply; }
    static void AutoApply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Path);
        if(scene.IsValid() && scene.isDirty) { Debug.Log("Performance authoring deferred: save the scene, then use ReTune/Scenes/Apply Wave Performance."); return; }
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        bool opened=!scene.IsValid();
        if(opened) scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        try { Apply(); } finally { if(opened) EditorSceneManager.CloseScene(scene,true); if(active.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(active); }
    }
    [MenuItem("ReTune/Scenes/Apply Wave Performance")]
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Path);
        if(!scene.IsValid()) scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        ReTuneSceneController c=null;
        foreach(var sceneRoot in scene.GetRootGameObjects()) { var candidate=sceneRoot.GetComponentInChildren<ReTuneSceneController>(true); if(candidate!=null)c=candidate; }
        if(c==null)throw new System.InvalidOperationException("Missing Performance controller");
        if(c.GetComponent<ReTunePerformanceStage>()!=null){ReTunePerformanceArtLayout.Apply(c.GetComponent<ReTunePerformanceStage>());EditorSceneManager.SaveScene(scene);return;}
        material=AssetDatabase.LoadAssetAtPath<Material>("Assets/ReTuneScenes/Art/PerformanceLine.mat");
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,"Assets/ReTuneScenes/Art/PerformanceLine.mat");}
        var s=c.gameObject.AddComponent<ReTunePerformanceStage>();s.controller=c;s.postmanBeatmap=AssetDatabase.LoadAssetAtPath<ReTuneBeatmap>("Assets/ReTuneScenes/Data/PostmanClickBeatmap.asset");
        GameObject world=null;foreach(var item in scene.GetRootGameObjects())if(item.name=="World")world=item;
        foreach(var tr in world.GetComponentsInChildren<Transform>(true))
            if(tr.name=="Balance Dial" || tr.name=="A_Melody Track" || tr.name=="B_Reply Track")tr.gameObject.SetActive(false);
        var root=new GameObject("Performance_WaveAndGear");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        s.track=Line("Semicircle Track",root.transform,new Color(.58f,.65f,.57f),.035f,20);
        s.track.positionCount=81;s.track.useWorldSpace=true;for(int i=0;i<=80;i++)s.track.SetPosition(i,s.Point(i*180f/80));
        s.targetGear=Gear("Target Gear",root.transform,"174143",.7f,21);s.targetGear.position=s.Point(90);
        s.playerGear=Gear("Player Gear",root.transform,"173613",.36f,22);s.playerGear.position=s.Point(40);
        s.waveB=Wave("WaveB_Reference",root.transform,new Color(.62f,.75f,.82f),25);
        s.waveA=Wave("WaveA_Progress",root.transform,new Color(.85f,.66f,.32f),26);
        s.waveA.DrawSharedWave(0,0,0,0);s.waveB.DrawSharedWave(0,1,0,0);
        var schedule=AssetDatabase.LoadAssetAtPath<ReTuneRandomQTESchedule>("Assets/ReTuneScenes/Data/PerformanceQTESchedule.asset");
        if(schedule==null){schedule=ScriptableObject.CreateInstance<ReTuneRandomQTESchedule>();AssetDatabase.CreateAsset(schedule,"Assets/ReTuneScenes/Data/PerformanceQTESchedule.asset");}s.schedule=schedule;
        if(c.needle!=null)c.needle.gameObject.SetActive(false);
        foreach(var text in Object.FindObjectsOfType<Text>(true))
        {
            if(text.gameObject.scene!=scene)continue;
            if(text.name=="Controls Hint")text.text="A / D: align the gears";
            if(text.name=="Timing Hint")text.text="Click when the rings meet.";
            if(text.name=="Green Zone Hint")text.text="Stay near the larger gear";
            if(text.name=="Progress Hint")text.text="Harmony";
            if(text.name=="A Key Label")text.text="A";
            if(text.name=="D Key Label")text.text="D";
        }
        if(c.stats!=null)c.stats.text="Harmony 0%\nPerfect 0";
        if(c.hint!=null)c.hint.text="Align the gears. Click when the rings meet.";
        if(c.holdFill!=null)c.holdFill.transform.parent.gameObject.SetActive(false);
        ReTunePerformanceArtLayout.Apply(s);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PERFORMANCE_WAVES_SAVED");
    }
    static LineRenderer Line(string name,Transform root,Color color,float width,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=false;l.startColor=l.endColor=color;l.startWidth=l.endWidth=width;l.sortingOrder=order;return l;
    }
    static Oscillogram Wave(string name,Transform root,Color color,int order)
    {
        var l=Line(name,root,color,.035f,order);l.transform.position=new Vector3(.65f,.7f,0);var wave=l.gameObject.AddComponent<Oscillogram>();wave.sharedCurve=l;wave.enabled=false;return wave;
    }
    static Transform Gear(string name,Transform root,string sprite,float size,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/PartsArt/"+sprite+".png");r.sortingOrder=order;float scale=size/r.sprite.bounds.size.x;go.transform.localScale=Vector3.one*scale;return go.transform;
    }
}




