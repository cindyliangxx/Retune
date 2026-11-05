using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneCompleteStoryValidation
{
    static ReTuneCompleteStoryValidation(){EditorApplication.delayCall+=Validate;}
    [MenuItem("ReTune/Scenes/Validate Complete Story")]
    public static void Validate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new List<string>();int errors=0;
        var book=JsonUtility.FromJson<ReTuneNarrativeBook>(File.ReadAllText("Assets/Resources/ReTuneNarrative.json"));
        if(book.customers.Length!=4)errors++;
        var story=AssetDatabase.LoadAssetAtPath<ReTuneStoryData>("Assets/ReTuneScenes/Data/Story.asset");
        int[] structures={0,1,2,3},tones={4,5,4,5};
        for(int i=0;i<4;i++)
        {
            var c=story.customers[i];if(c.structure!=structures[i]||c.tone!=tones[i]){errors++;report.Add("ERROR: recipe "+i);}
            if(book.customers[i].dialogue.Length<8||book.customers[i].reply.Length<4||book.customers[i].departure.Length<2){errors++;report.Add("ERROR: incomplete visitor "+i);}
            report.Add(c.name+": "+book.customers[i].dialogue.Length+" dialogue / "+book.customers[i].reply.Length+" reply / "+book.customers[i].departure.Length+" departure pages; recipe "+c.structure+" + "+c.tone);
        }
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(string name in new[]{"00_Opening","00_Start","01_Shop","02_Parts","03_Performance","04_Reply","05_Ending"})
        {
            string path="Assets/ReTuneScenes/Scenes/"+name+".unity";
            if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==path)){errors++;report.Add("ERROR: build scene "+name);}
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{
                var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var flow=components.OfType<ReTuneNarrativePlayer>().FirstOrDefault();
                if(name!="02_Parts"&&name!="03_Performance")
                {
                    if(flow==null||flow.body==null||flow.nextButton==null||flow.backButton==null){errors++;report.Add("ERROR: narrative UI "+name);}
                    else
                    {
                        IEnumerable<string> pages=flow.mode==ReTuneNarrativePlayer.Mode.Opening?book.opening:flow.mode==ReTuneNarrativePlayer.Mode.Ending?book.ending:flow.mode==ReTuneNarrativePlayer.Mode.Shop?book.customers.SelectMany(c=>c.dialogue):flow.mode==ReTuneNarrativePlayer.Mode.Reply?book.customers.SelectMany(c=>c.reply.Concat(c.departure)):new[]{"A small shop. A place to listen."};
                        Canvas.ForceUpdateCanvases();float maxHeight=0;
                        foreach(var text in pages){var settings=flow.body.GetGenerationSettings(new Vector2(flow.body.rectTransform.rect.width,0));float height=flow.body.cachedTextGeneratorForLayout.GetPreferredHeight(text,settings)/flow.body.pixelsPerUnit;maxHeight=Mathf.Max(maxHeight,height);}
                        if(maxHeight>flow.body.rectTransform.rect.height+2){errors++;report.Add("ERROR: text overflow "+name+" needs "+maxHeight+" / "+flow.body.rectTransform.rect.height);}
                        report.Add(name+": narrative UI wired, maximum body height "+maxHeight.ToString("0"));
                    }
                }
                if(name=="02_Parts"){var c=components.OfType<ReTuneSceneController>().FirstOrDefault();if(c==null||c.structureSlot==null||c.toneSlot==null||c.partObjects.Length!=6){errors++;report.Add("ERROR: parts references");}}
                if(name=="03_Performance"){var stage=components.OfType<ReTunePerformanceStage>().FirstOrDefault();if(stage==null||stage.waveA==null||stage.waveB==null||stage.pointerVisual==null){errors++;report.Add("ERROR: performance references");}}
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
        report.Insert(0,"Complete story validation: "+errors+" errors");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/CompleteStory-validation.txt",report);Debug.Log("RETUNE_STORY_VALIDATION: "+errors+" errors");
    }
}
