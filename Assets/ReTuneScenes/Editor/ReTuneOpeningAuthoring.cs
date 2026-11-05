using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneOpeningAuthoring
{
    const string Art="Assets/ReTuneScenes/Art/Opening/Storyboard/";
    static ReTuneOpeningAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Apply Opening Storyboards")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/ReTuneScenes/Scenes/00_Opening.unity";var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
        if(!opened&&scene.isDirty){Debug.LogWarning("Opening storyboard deferred: save the scene then apply opening storyboards.");return;}
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var flow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneNarrativePlayer>(true)).FirstOrDefault();if(flow==null)return;
            var existing=flow.GetComponent<ReTuneOpeningStoryboard>();if(existing!=null){existing.EnsureSubtitleBackdrop();existing.EnsureLetterLayout();existing.ConfigurePresentation();EditorSceneManager.SaveScene(scene);return;}
            var storyboard=flow.gameObject.AddComponent<ReTuneOpeningStoryboard>();
            var parent=flow.panel.transform.parent;
            if(flow.openingArtwork!=null)flow.openingArtwork.SetActive(false);flow.openingArtwork=null;
            var root=Rect("Opening Storyboard Art",parent);root.SetAsFirstSibling();
            var black=root.gameObject.AddComponent<Image>();black.color=new Color(1,1,.91f);black.raycastTarget=false;
            string[][] layers={new[]{"清晨街道.png"},new[]{"进入店铺-左上1.png"},new[]{"进入店铺-左上1.png","进入店铺-中间2.png"},new[]{"进入店铺-底下3.png","进入店铺-中间2.png","进入店铺-左上1.png"},new[]{"冷清店面透明底.png"},new[]{"桌面信件透明底.png"},new[]{"LetterPaper.png"},new[]{"LetterPaper.png"},new[]{"桌面信件透明底.png"}};
            string[] names={"01 Morning Street","02 Inherited Keys","03 Unlocking","04 Opening Door","05 Quiet Shop","06 Letter on Counter","07 Read Letter","08 Promise","09 Music Box"};
            storyboard.shots=new GameObject[layers.Length];
            for(int i=0;i<layers.Length;i++){var shot=Rect(names[i],root);storyboard.shots[i]=shot.gameObject;foreach(string layer in layers[i])Image(layer,shot,Sprite(layer));shot.gameObject.SetActive(i==0);}
            storyboard.sound=flow.gameObject.AddComponent<AudioSource>();storyboard.sound.playOnAwake=false;storyboard.sound.spatialBlend=0;
            storyboard.unlockSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/音频/音效/开门声_02.wav");storyboard.doorSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/音频/音效/开门声_01.ogg");storyboard.openSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/音频/音效/八音盒盒盖开启.wav");
            storyboard.dialoguePanel=flow.panel.GetComponent<Image>();storyboard.narrativeBody=flow.body;
            var panel=(RectTransform)flow.panel.transform;Place(panel,40,470,1200,230);
            flow.heading.gameObject.SetActive(false);Place(flow.body.rectTransform,30,8,1140,130);
            Place(flow.backButton.GetComponent<RectTransform>(),20,150,250,68);Place(flow.nextButton.GetComponent<RectTransform>(),820,150,350,68);Place(flow.pageLabel.rectTransform,490,150,200,68);
            var go=new GameObject("Letter Body",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.layer=5;var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);Place(rect,430,185,690,335);
            var text=go.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/LeMiShiguang.ttf");text.fontSize=50;text.color=new Color(.3f,.22f,.14f);text.raycastTarget=false;text.text="Dear friend,\n\nWhen this letter reaches you, I'll probably be gone. Thanks for taking the shop from me. Promise you'll keep it going.";storyboard.letterText=text;go.SetActive(false);
            flow.body.text="Morning light reaches the old street. A weathered sign above a quiet shop reads Re:Tune.";flow.pageLabel.text="1 / 9";
            storyboard.EnsureSubtitleBackdrop();storyboard.EnsureLetterLayout();storyboard.ConfigurePresentation();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("RETUNE_OPENING_STORYBOARDS_SAVED");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
    static RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static void Image(string name,Transform parent,Sprite sprite){var r=Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;}
    static void Place(RectTransform rect,float x,float y,float width,float height){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    static Sprite Sprite(string filename){string path=Art+filename;var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
}

