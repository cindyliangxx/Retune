using System;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.EditorCoroutines.Editor;
using ReTune.Scenes;
public static class ReTuneAudioRepair
{
    [MenuItem("ReTune/Audio/Apply Story and Customer Audio")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop play mode first.");
        const string path="Assets/Resources/ReTuneAudioLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<ReTuneAudioLibrary>(path);
        if(library==null){library=ScriptableObject.CreateInstance<ReTuneAudioLibrary>();AssetDatabase.CreateAsset(library,path);}
        library.postman=LoadStems("邮差","(1)");library.guitarist=LoadStems("吉他手","");library.bookshopOwner=LoadStems("书店老板","");
        library.menuMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Background/LoFiBGM.wav");
        library.dialogueMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Background/LoFiBGM.wav");
        library.endingMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/音频/BGM/人员名单_bgm.mp3");
        if(library.menuMusic==null||library.dialogueMusic==null||library.endingMusic==null||library.postman.Any(c=>c==null)||library.guitarist.Any(c=>c==null)||library.bookshopOwner.Any(c=>c==null))throw new Exception("Missing audio asset");
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        var active=SceneManager.GetActiveScene();
        foreach(var name in new[]{"01_Shop","02_Parts","03_Performance","04_Reply","05_Ending"})
        {
            var scene=SceneManager.GetSceneByPath("Assets/ReTuneScenes/Scenes/"+name+".unity");bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene("Assets/ReTuneScenes/Scenes/"+name+".unity",OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name=="Title").ToArray())UnityEngine.Object.DestroyImmediate(button.gameObject);
                foreach(var c in root.GetComponentsInChildren<ReTuneSceneController>(true)){c.postmanStems=library.postman;c.guitaristStems=library.guitarist;c.bookshopOwnerStems=library.bookshopOwner;EditorUtility.SetDirty(c);}
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
        Debug.Log("RETUNE_AUDIO_CONFIGURED: story BGM, customer stems and Title removal saved.");
    }
    static AudioClip[] LoadStems(string customer,string suffix)=>new[]{"A","B","C"}.Select(t=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/音频/分轨配乐/"+customer+"/"+customer+"_"+t+suffix+".wav")).ToArray();
    [MenuItem("ReTune/Testing/Audio/Verify Runtime Routes")]
    static void Verify(){if(!Application.isPlaying)throw new Exception("Enter Play mode first.");EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
    static IEnumerator Check()
    {
        string saved=PlayerPrefs.GetString("ReTune.Progress.v1","");bool had=PlayerPrefs.HasKey("ReTune.Progress.v1");
        var report=new System.Collections.Generic.List<string>();bool background=Application.runInBackground;Application.runInBackground=true;
        try
        {
            for(int customer=0;customer<3;customer++)foreach(string name in new[]{"02_Parts","03_Performance"})
            {
                ReTuneSession.Begin(customer);
                SceneManager.LoadScene("Assets/ReTuneScenes/Scenes/"+name+".unity");
                yield return new EditorWaitForSeconds(1.5f);
                var c=UnityEngine.Object.FindObjectOfType<ReTuneSceneController>();
                if(c==null)throw new Exception("Controller missing");
                c.CloseOverlay();if(name=="02_Parts")c.OriginalMelody();
                yield return new EditorWaitForSeconds(.5f);
                string expected=customer==0?"邮差":customer==1?"吉他手":"书店老板";
                if(!c.trackA.clip.name.StartsWith(expected)||!c.trackB.clip.name.StartsWith(expected)||!c.trackC.clip.name.StartsWith(expected))throw new Exception("Wrong customer audio in "+name);
                if(name=="03_Performance"&&(!c.trackA.isPlaying||!c.trackB.isPlaying||c.trackB.volume<=0||c.trackC.volume!=0||c.trackC.isPlaying))throw new Exception("Performance is not A+B only");
                if(!ReTuneBackgroundMusic.Instance.IsBlocked)throw new Exception("BGM overlapping gameplay");
                report.Add(expected+" / "+name+": "+c.trackA.clip.name+", "+c.trackB.clip.name+", A playing="+c.trackA.isPlaying+", B playing="+c.trackB.isPlaying+", C volume="+c.trackC.volume+", C playing="+c.trackC.isPlaying);
            }
            foreach(string name in new[]{"01_Shop","04_Reply","05_Ending"})
            {
                SceneManager.LoadScene("Assets/ReTuneScenes/Scenes/"+name+".unity");
                yield return new EditorWaitForSeconds(1.5f);
                var bgm=ReTuneBackgroundMusic.Instance;var source=bgm.GetComponent<AudioSource>();
                if(bgm.IsBlocked||!source.isPlaying||source.clip==null)throw new Exception("Story BGM not playing in "+name);
                var c=UnityEngine.Object.FindObjectOfType<ReTuneSceneController>();
                if(c!=null&&(c.trackA.isPlaying||c.trackB.isPlaying||c.trackC.isPlaying))throw new Exception("Gameplay stems overlapping story music");
                report.Add(name+": BGM="+source.clip.name+", playing="+source.isPlaying+", volume="+source.volume);
            }
            System.IO.File.WriteAllLines("Logs/AudioRoutes-runtime-validation.txt",report);
            Debug.Log("RETUNE_AUDIO_RUNTIME_PASS: "+string.Join("; ",report));
        }
        finally{Application.runInBackground=background;if(had)PlayerPrefs.SetString("ReTune.Progress.v1",saved);else PlayerPrefs.DeleteKey("ReTune.Progress.v1");PlayerPrefs.Save();}
    }
}



