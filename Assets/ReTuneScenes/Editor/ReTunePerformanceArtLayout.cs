using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using ReTune.Scenes;
public static class ReTunePerformanceArtLayout
{
    const string Root="Assets/ReTuneScenes/Art/PerformanceLayers/";
    static RectTransform parent;
    static Sprite Import(string filename)
    {
        string path=Root+filename;
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=150;importer.isReadable=true;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.maxTextureSize=2048;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Image Image(string name,Sprite sprite)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;
        var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(1280,720);
        var image=go.GetComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;
    }
    static Image Layer(string name,string file)=>Image(name,Import(file+".png"));
    // 保留原画布坐标，将局部素材转换成有准确点击/填充范围的 Image。
    static Image Cropped(string name,string file)
    {
        var sprite=Import(file+".png");var texture=sprite.texture;var pixels=texture.GetPixels32();int left=texture.width,bottom=texture.height,right=0,top=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>20){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
        var bounds=new Rect(left,bottom,right-left+1,top-bottom+1);var crop=Sprite.Create(texture,bounds,Vector2.one*.5f,150);
        string asset=Root+name+".asset";AssetDatabase.CreateAsset(crop,asset);
        var image=Image(name,crop);var rect=image.rectTransform;rect.sizeDelta=bounds.size*(2f/3);rect.anchoredPosition=(bounds.center-new Vector2(texture.width,texture.height)*.5f)*(2f/3);return image;
    }
    public static void Apply(ReTunePerformanceStage stage)
    {
        var scene=stage.gameObject.scene;var c=stage.controller;
        foreach(var root in scene.GetRootGameObjects())if(root.name=="Performance_Illustration_V3")return;
        foreach(var root in scene.GetRootGameObjects())if(root.name=="World" || root.name=="Performance_Illustration")root.SetActive(false);
        var canvas=c.overlay.GetComponentInParent<Canvas>().rootCanvas;
        foreach(Transform child in canvas.transform)if(child.gameObject!=c.overlay)child.gameObject.SetActive(false);
        var group=new GameObject("Performance_Illustration_V3",typeof(RectTransform));parent=(RectTransform)group.transform;parent.SetParent(canvas.transform,false);parent.anchorMin=Vector2.zero;parent.anchorMax=Vector2.one;parent.offsetMin=parent.offsetMax=Vector2.zero;parent.SetAsFirstSibling();
        Image("Background",Import("Background.jpg"));
        Layer("Decorative gears","2026-11-04 025141");Layer("Dial","2026-11-04 025214");
        var blue=Layer("Target range","2026-11-04 015533");blue.gameObject.SetActive(false);
        Layer("Progress frame","2026-11-03 214752");
        var fill=Cropped("HarmonyFill","2026-11-04 025009");fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.fillAmount=0;c.phraseFill=fill;
        var pointer=Layer("Player needle","2026-11-04 015556").rectTransform;pointer.pivot=new Vector2(.5f,0);pointer.anchoredPosition=new Vector2(0,-360);stage.pointerVisual=pointer;
        Layer("Earphones","2026-11-03 214723");
        var note=Cropped("QTE_Note","2026-11-03 214739");note.raycastTarget=true;var button=note.gameObject.AddComponent<Button>();button.targetGraphic=note;UnityEventTools.AddPersistentListener(button.onClick,c.NoteDown);
        var ring=Image("Approach ring",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ReTuneScenes/UI/Ring.png"));ring.rectTransform.sizeDelta=note.rectTransform.sizeDelta*1.15f;ring.rectTransform.anchoredPosition=note.rectTransform.anchoredPosition;ring.color=new Color(1,.85f,.4f,1);c.approachRing=ring.rectTransform;
        var target=Layer("Target indicator","2026-11-04 015556");target.color=new Color(.45f,.8f,1,.65f);target.rectTransform.pivot=new Vector2(.5f,0);target.rectTransform.anchoredPosition=new Vector2(0,-360);stage.targetVisual=target.rectTransform;
        stage.trackCenter=new Vector2(0,-3.6f);stage.trackRadius=2.15f;
        if(stage.track!=null)stage.track.enabled=false;
        stage.playerGear.gameObject.SetActive(false);stage.targetGear.gameObject.SetActive(false);
        if(stage.waveA!=null){stage.waveA.transform.position=new Vector3(-5.2f,1.3f,0);stage.waveA.curveWidth=10.4f;stage.waveA.curveAmplitude=.55f;}
        if(stage.waveB!=null){stage.waveB.transform.position=new Vector3(-5.2f,1.3f,0);stage.waveB.curveWidth=10.4f;stage.waveB.curveAmplitude=.55f;}
        // 世界波浪通过摄像机画面显示在 UI 背景之上。
        var waveCanvas=new GameObject("Wave view",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));waveCanvas.layer=5;var wr=(RectTransform)waveCanvas.transform;wr.SetParent(parent,false);wr.anchorMin=wr.anchorMax=wr.pivot=Vector2.one*.5f;wr.sizeDelta=new Vector2(1060,180);wr.anchoredPosition=new Vector2(0,132);
        var cameraObject=new GameObject("Performance wave camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.GetComponent<Camera>();camera.transform.position=new Vector3(0,1.3f,-10);camera.orthographic=true;camera.orthographicSize=.9f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<30;
        stage.waveA.gameObject.layer=stage.waveB.gameObject.layer=30;
        var rt=new RenderTexture(1060,180,16);rt.name="PerformanceWaveTexture";AssetDatabase.CreateAsset(rt,Root+"PerformanceWaveTexture.asset");camera.targetTexture=rt;waveCanvas.GetComponent<RawImage>().texture=rt;waveCanvas.GetComponent<RawImage>().raycastTarget=false;waveCanvas.transform.SetSiblingIndex(1);
        if(c.worldCamera!=null)c.worldCamera.cullingMask &= ~(1<<30);
        c.stats=null;c.noteLabel=null;c.stabilityFill=null;c.responseFill=null;c.holdFill=null;c.hint=null;c.mixLabel=null;
        stage.waveA.DrawSharedWave(0,0,0,0);stage.waveB.DrawSharedWave(0,1,0,0);AssetDatabase.SaveAssets();
    }
}



