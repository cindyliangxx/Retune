using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneDialZoneAuthoring
{
    static ReTuneDialZoneAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Apply Dial Precision Zones")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/ReTuneScenes/Scenes/03_Performance.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!opened && scene.isDirty)return;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            ReTunePerformanceStage stage=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<ReTunePerformanceStage>(true);if(found!=null)stage=found;}
            if(stage==null || stage.pointerVisual==null || stage.broadZone!=null)return;
            stage.tolerance=18;stage.preciseTolerance=5;stage.preciseMultiplier=1.8f;
            stage.broadZone=Zone("Broad alignment zone",stage);stage.preciseZone=Zone("Precise alignment zone",stage);
            if(stage.targetVisual!=null)stage.targetVisual.gameObject.SetActive(false);
            stage.broadZone.SetRange(stage.targetAngle,stage.tolerance,new Color(.3f,.65f,.9f,.5f));stage.preciseZone.SetRange(stage.targetAngle,stage.preciseTolerance,new Color(1,.83f,.32f,.85f));
            EditorSceneManager.SaveScene(scene);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
    static ReTuneDialZone Zone(string name,ReTunePerformanceStage stage)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(ReTuneDialZone));go.layer=5;
        var rect=(RectTransform)go.transform;rect.SetParent(stage.pointerVisual.parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.zero;
        rect.SetSiblingIndex(stage.pointerVisual.GetSiblingIndex());var zone=go.GetComponent<ReTuneDialZone>();zone.raycastTarget=false;return zone;
    }
}
