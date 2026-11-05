using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneDialogueAuthoring
{
    static ReTuneDialogueAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Apply Floating Dialogue")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(string name in new[]{"01_Shop","04_Reply"})
        {
            string path="Assets/ReTuneScenes/Scenes/"+name+".unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
            if(!opened&&scene.isDirty){Debug.LogWarning("Save "+name+" before applying floating dialogue.");continue;}
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var flow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneNarrativePlayer>(true)).FirstOrDefault();if(flow==null)continue;
                var ui=flow.GetComponent<ReTuneDialoguePresentation>();if(ui==null)ui=flow.gameObject.AddComponent<ReTuneDialoguePresentation>();
                ui.player=flow;ui.Configure();
                var book=ReTuneNarrativeBook.Load();int customer=Mathf.Clamp(flow.controller.previewCustomer,0,3);
                ui.Show(name=="01_Shop"?book.customers[customer].dialogue[0]:book.customers[customer].reply[0]);
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("RETUNE_FLOATING_DIALOGUE_SAVED");
    }
}
