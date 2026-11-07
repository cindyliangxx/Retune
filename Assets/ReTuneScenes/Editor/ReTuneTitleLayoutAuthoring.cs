using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneTitleLayoutAuthoring
{
    static ReTuneTitleLayoutAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Align Title to Original Cover")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/ReTuneScenes/Scenes/00_Start.unity";
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
        if(!opened&&scene.isDirty){Debug.LogWarning("Save the title scene before aligning its cover.");return;}
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var flow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneNarrativePlayer>(true)).FirstOrDefault();
            if(flow==null)return;
            var cover=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Image>(true)).First(i=>i.name=="Title Artwork");
            cover.sprite=Background();
            var logo=cover.transform.Find("Original Glowing Logo");
            if(logo==null){var go=new GameObject("Original Glowing Logo",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(cover.transform,false);logo=go.transform;}
            var logoRect=(RectTransform)logo;logoRect.anchorMin=Vector2.zero;logoRect.anchorMax=Vector2.one;logoRect.offsetMin=logoRect.offsetMax=Vector2.zero;
            var logoImage=logo.GetComponent<Image>();logoImage.sprite=Import("TitleLogo.png");logoImage.preserveAspect=true;logoImage.raycastTarget=false;logo.SetAsFirstSibling();
            var panel=(RectTransform)flow.panel.transform;
            panel.name="Title Artwork Hotspots";panel.SetParent(cover.transform,false);
            panel.anchorMin=Vector2.zero;panel.anchorMax=Vector2.one;panel.offsetMin=panel.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=Color.clear;panel.GetComponent<Image>().raycastTarget=false;
            foreach(Transform child in panel)child.gameObject.SetActive(false);
            var play=flow.nextButton;play.name="New Game";play.gameObject.SetActive(true);
            Label(play,"New Game",flow.nextLabel.font);
            var review=panel.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="Replay Opening"||b.name=="Review");
            if(review!=null){review.name="Review";review.gameObject.SetActive(true);Label(review,"Review",flow.nextLabel.font);}
            var tape=panel.Find("Tape");
            if(tape==null){var go=new GameObject("Tape",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));go.layer=5;go.transform.SetParent(panel,false);tape=go.transform;}
            tape.gameObject.SetActive(true);
            var tapeButton=tape.GetComponent<Button>();tapeButton.interactable=true;
            while(tapeButton.onClick.GetPersistentEventCount()>0)UnityEditor.Events.UnityEventTools.RemovePersistentListener(tapeButton.onClick,0);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(tapeButton.onClick,flow.ResumeGame);
            Label(tape.GetComponent<Button>(),"Tape",flow.nextLabel.font);
            foreach(var button in panel.GetComponentsInChildren<Button>(true)){button.transition=Selectable.Transition.None;var image=button.GetComponent<Image>();if(image!=null){image.color=Color.clear;image.raycastTarget=button.interactable;}}
            var layout=cover.GetComponent<ReTuneTitleHotspots>();if(layout==null)layout=cover.gameObject.AddComponent<ReTuneTitleHotspots>();
            layout.cover=cover;layout.newGame=(RectTransform)play.transform;layout.review=review!=null?(RectTransform)review.transform:null;layout.tape=(RectTransform)tape;
            Canvas.ForceUpdateCanvases();layout.Align();
            ReTuneTitleMotionAuthoring.Configure(cover,logoRect,new[]{layout.newGame,layout.review,layout.tape});
            EditorSceneManager.SaveScene(scene);Debug.Log("RETUNE_TITLE_ORIGINAL_POSITIONS_SAVED");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
    static Sprite Import(string name)
    {
        string path="Assets/ReTuneScenes/Art/Title/"+name;
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite Background()
    {
        const string asset="Assets/ReTuneScenes/Art/Title/TitleBackground_Crop.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(existing!=null)return existing;
        var source=Import("TitleBackground.jpg");
        // Exclude the drawing-app grid border, then center-crop to the original 16:9 cover.
        var texture=source.texture;float h=texture.height*(636f/691f),w=h*16/9;
        var sprite=Sprite.Create(texture,new Rect(texture.width*.5043f-w/2,texture.height*(21f/691f),w,h),new Vector2(.5f,.5f),100);
        sprite.name="TitleBackground_Crop";AssetDatabase.CreateAsset(sprite,asset);return sprite;
    }
    static void Label(Button button,string value,Font font)
    {
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/LeMiShiguang.ttf")??font;
        var text=button.GetComponentInChildren<Text>(true);
        if(text==null){var go=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.layer=5;go.transform.SetParent(button.transform,false);text=go.GetComponent<Text>();}
        text.gameObject.SetActive(true);text.font=font;text.fontStyle=FontStyle.Bold;text.text=value;text.fontSize=88;text.resizeTextForBestFit=false;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.97f,.82f);text.raycastTarget=false;
        var rect=text.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        if(text.GetComponent<ReTuneTextGlow>()==null)text.gameObject.AddComponent<ReTuneTextGlow>();
    }
    public static void ApplyOpeningAndTitle(){ReTuneOpeningAuthoring.Apply();Apply();AssetDatabase.SaveAssets();}
}
