using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneSeatedVisitorsAuthoring
{
    static ReTuneSeatedVisitorsAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Seat Visitors Behind Desk")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(string name in new[]{"01_Shop","04_Reply"})
        {
            string path="Assets/ReTuneScenes/Scenes/"+name+".unity";var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
            if(!opened&&scene.isDirty){Debug.LogWarning("Save "+name+" before seating visitors.");continue;}
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var flow=roots.SelectMany(r=>r.GetComponentsInChildren<ReTuneNarrativePlayer>(true)).First();
                var source=roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).FirstOrDefault(r=>r.sprite!=null&&r.sprite.texture.name=="主场景1-前景");
                var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
                var placement=flow.GetComponent<ReTuneSeatedVisitors>();bool created=placement==null;
                if(created)placement=flow.gameObject.AddComponent<ReTuneSeatedVisitors>();
                placement.foregroundSource=source;placement.sceneCamera=camera;
                placement.useLayeredRoom=true;
                placement.useSeparateDeskLayers=true;
                if(placement.clippingFrame==null)
                {
                    var frame=new GameObject("Desk Foreground Occlusion",typeof(RectTransform),typeof(RectMask2D));frame.layer=5;frame.transform.SetParent(flow.visitorPortraits.transform.parent,false);placement.clippingFrame=(RectTransform)frame.transform;
                    var go=new GameObject("Desk Artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(frame.transform,false);placement.deskForeground=go.GetComponent<Image>();
                }
                const string art="Assets/ReTuneScenes/Art/Title/Layers/";
                placement.deskForeground.sprite=ImportDeskLayer("DeskWithoutChair.png");
                if(placement.deskCup==null)
                {
                    var go=new GameObject("Desk Cup",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(placement.clippingFrame,false);placement.deskCup=go.GetComponent<Image>();
                }
                placement.deskCup.sprite=ImportDeskLayer("DeskCup.png");placement.deskCup.transform.SetAsLastSibling();
                placement.clippingFrame.SetSiblingIndex(flow.visitorPortraits.transform.GetSiblingIndex()+1);
                if(placement.roomBackground==null)
                {
                    var go=new GameObject("Seated Visitors Room",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(flow.visitorPortraits.transform.parent,false);go.transform.SetSiblingIndex(flow.visitorPortraits.transform.GetSiblingIndex());
                    placement.roomBackground=go.GetComponent<Image>();
                }
                placement.roomBackground.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art+"主场景1-完整.png");
                string[] layers={"主场景1-后面.png","主场景1-中景.png","主场景1-前景.png"};
                for(int i=0;i<layers.Length;i++)
                {
                    var layer=placement.roomBackground.transform.Find(layers[i]);
                    if(layer==null){var go=new GameObject(layers[i],typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(placement.roomBackground.transform,false);layer=go.transform;}
                    var rect=(RectTransform)layer;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;layer.SetSiblingIndex(i);
                    var image=layer.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art+layers[i]);image.raycastTarget=false;image.preserveAspect=false;
                }
                placement.deskForeground.raycastTarget=false;
                // Only initialize placement once, so later manual adjustments survive.
                if(created)
                {
                    float[] heights={560,570,550,570},tops={80,70,90,70};
                    for(int i=0;i<flow.controller.portraits.Length;i++)
                    {
                        var rect=(RectTransform)flow.controller.portraits[i].transform;
                        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(i==3?-370:-420,-tops[i]);rect.sizeDelta=new Vector2(i==3?430:480,heights[i]);
                    }
                }
                Canvas.ForceUpdateCanvases();placement.AlignForeground();
                EditorSceneManager.SaveScene(scene);Debug.Log("RETUNE_SEATED_VISITORS_SAVED "+name);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
    }
    static Sprite ImportDeskLayer(string name)
    {
        string path="Assets/ReTuneScenes/Art/DialogueLayers/"+name;
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)return null;
        if(importer.textureType!=TextureImporterType.Sprite||importer.maxTextureSize!=4096)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
