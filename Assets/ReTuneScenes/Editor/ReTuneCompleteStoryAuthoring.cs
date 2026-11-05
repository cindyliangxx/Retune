using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneCompleteStoryAuthoring
{
    const string Root="Assets/ReTuneScenes/Scenes/";
    const string Version="CompleteStoryUI_V1";
    static Font font;
    static ReTuneCompleteStoryAuthoring(){EditorApplication.delayCall+=ApplyAll;}
    [MenuItem("ReTune/Scenes/Connect Complete Story")]
    public static void ApplyAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/LeMiShiguang.ttf");
        var story=AssetDatabase.LoadAssetAtPath<ReTuneStoryData>("Assets/ReTuneScenes/Data/Story.asset");
        var book=JsonUtility.FromJson<ReTuneNarrativeBook>(System.IO.File.ReadAllText("Assets/Resources/ReTuneNarrative.json"));
        for(int i=0;i<4;i++){story.customers[i].dialogue=book.customers[i].dialogue;story.customers[i].reply=string.Join("\n\n",book.customers[i].reply);}EditorUtility.SetDirty(story);
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        string[] names={"00_Opening","00_Start","01_Shop","04_Reply","05_Ending"};
        for(int i=0;i<names.Length;i++)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Root+names[i]+".unity");bool opened=!scene.IsValid();
            if(!opened&&scene.isDirty){Debug.LogWarning("Complete story: save "+names[i]+" then run Connect Complete Story.");continue;}
            if(opened)scene=EditorSceneManager.OpenScene(Root+names[i]+".unity",OpenSceneMode.Additive);
            try{Author(scene,i);}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
        var scenes=EditorBuildSettings.scenes.ToList();scenes.RemoveAll(s=>s.path==Root+"00_Opening.unity");scenes.Insert(0,new EditorBuildSettingsScene(Root+"00_Opening.unity",true));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();Debug.Log("RETUNE_COMPLETE_STORY_CONNECTED");
    }
    static void Author(UnityEngine.SceneManagement.Scene scene,int mode)
    {
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name==Version||t.name=="Title Artwork Hotspots"))return;
        var controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneSceneController>(true)).FirstOrDefault();
        var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(c=>c.renderMode==RenderMode.ScreenSpaceOverlay);
        if(canvas==null){var go=new GameObject("NarrativeCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;}
        if(canvas.GetComponent<GraphicRaycaster>()==null)canvas.gameObject.AddComponent<GraphicRaycaster>();
        var scaler=canvas.GetComponent<CanvasScaler>();if(scaler==null)scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        if(mode!=0)foreach(Transform child in canvas.transform)if(controller==null||child.gameObject!=controller.overlay)child.gameObject.SetActive(false);
        foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.GetComponentInParent<Canvas>()!=canvas)text.enabled=false;
        var panel=Box(canvas.transform,Version,40,280,1200,410,new Color(.94f,.88f,.76f,1));
        var owner=controller!=null?controller.gameObject:new GameObject("Narrative Player");if(controller==null)UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner,scene);
        var flow=owner.AddComponent<ReTuneNarrativePlayer>();flow.controller=controller;flow.panel=panel.gameObject;flow.mode=(ReTuneNarrativePlayer.Mode)mode;
        // 顺序：Opening、Title、Shop、Reply、Ending。
        flow.heading=Text(panel,"Heading","Re:Tune",30,16,1140,66);
        flow.body=Text(panel,"Narrative","",30,85,1140,230);flow.body.supportRichText=false;
        flow.pageLabel=Text(panel,"Page","",480,330,180,60);flow.pageLabel.alignment=TextAnchor.MiddleCenter;
        flow.backButton=Button(panel,"Previous",30,322,250,76,flow.Previous);
        flow.nextButton=Button(panel,"Next",800,322,370,76,flow.Next);flow.nextLabel=flow.nextButton.GetComponentInChildren<Text>();
        if(controller!=null&&controller.overlay!=null)controller.overlay.transform.SetAsLastSibling();
        if(mode==0){flow.openingArtwork=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Morning Street - Shot 01")?.gameObject;}
        if(mode==1)
        {
            var path="Assets/ReTuneScenes/Art/Title/TitleCover.jpg";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
            var cover=Box(canvas.transform,"Title Artwork",0,0,1280,720,Color.white);cover.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);cover.GetComponent<Image>().preserveAspect=true;cover.SetAsFirstSibling();
            panel.anchoredPosition=new Vector2(870,-50);panel.sizeDelta=new Vector2(380,620);
            Set(flow.heading.rectTransform,25,20,330,80);Set(flow.body.rectTransform,25,110,330,180);Set(flow.nextButton.GetComponent<RectTransform>(),20,320,340,80);
            Button(panel,"Replay Opening",20,420,340,80,flow.ReplayOpening);Button(panel,"Step Out",20,520,340,80,flow.StepOut);flow.pageLabel.gameObject.SetActive(false);
        }
        if(mode==2||mode==3)Button(canvas.transform,"Title",1030,20,220,76,flow.ReturnToTitle);
        if(!scene.GetRootGameObjects().Any(r=>r.GetComponent<UnityEngine.EventSystems.EventSystem>()!=null))
        {var events=new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(events,scene);}
        EditorSceneManager.SaveScene(scene);
    }
    static void Set(RectTransform rect,float x,float y,float w,float h){rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
    static RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);Set(rect,x,y,w,h);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
    }
    static Text Text(Transform parent,string name,string value,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.layer=5;var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);Set(rect,x,y,w,h);var text=go.GetComponent<Text>();text.font=font;text.fontSize=50;text.color=new Color(.29f,.22f,.20f);text.text=value;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
    }
    static Button Button(Transform parent,string label,float x,float y,float w,float h,UnityAction action)
    {
        var rect=Box(parent,label,x,y,w,h,new Color(.84f,.74f,.59f));rect.GetComponent<Image>().raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();var text=Text(rect,"Label",label,8,3,w-16,h-6);text.alignment=TextAnchor.MiddleCenter;UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
}
