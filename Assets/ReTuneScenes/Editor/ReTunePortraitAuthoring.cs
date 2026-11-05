using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTunePortraitAuthoring
{
    const string Art="Assets/ReTuneScenes/Art/Portraits/";
    static ReTunePortraitAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Apply Visitor Art and Title Glow")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(string name in new[]{"01_Shop","04_Reply","00_Start"})
        {
            string path="Assets/ReTuneScenes/Scenes/"+name+".unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
            if(!opened&&scene.isDirty){Debug.LogWarning("Visitor art deferred: save "+name+" then run Apply Visitor Art and Title Glow.");continue;}
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var flow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneNarrativePlayer>(true)).FirstOrDefault();if(flow==null)continue;
                if(name=="00_Start")
                {
                    if(flow.panel.name=="Title Artwork Hotspots")continue;
                    flow.panel.GetComponent<Image>().color=new Color(.3f,.22f,.14f,.94f);
                    flow.body.color=new Color(.96f,.89f,.75f);
                    foreach(var text in flow.panel.GetComponentsInChildren<Text>(true))
                    {
                        if(text==flow.heading||text.name=="Label")
                        {
                            if(text.GetComponent<ReTuneTextGlow>()==null)text.gameObject.AddComponent<ReTuneTextGlow>();
                            text.color=new Color(1,.97f,.82f);
                            var button=text.GetComponentInParent<Button>();if(button!=null)button.GetComponent<Image>().color=new Color(.38f,.28f,.17f,.95f);
                        }
                    }
                }
                else if(flow.visitorPortraits==null)
                {
                    var controller=flow.controller;foreach(var old in controller.portraits)if(old!=null)old.SetActive(false);
                    var parent=flow.panel.transform.parent;
                    var group=new GameObject("VisitorPortraits_Art",typeof(RectTransform));var rect=(RectTransform)group.transform;rect.SetParent(parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;group.layer=5;rect.SetSiblingIndex(flow.panel.transform.GetSiblingIndex());
                    flow.visitorPortraits=group;controller.portraits=new GameObject[4];
                    string[] visitors={"Postman","Guitarist","BookshopOwner","Photographer"};
                    for(int i=0;i<4;i++)
                    {
                        var go=new GameObject("Portrait_"+visitors[i],typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;var image=go.GetComponent<Image>();image.sprite=Portrait(visitors[i]);image.preserveAspect=true;image.raycastTarget=false;
                        var r=(RectTransform)go.transform;r.SetParent(rect,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-12);r.sizeDelta=new Vector2(580,650);
                        controller.portraits[i]=go;go.SetActive(i==controller.previewCustomer);
                    }
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("RETUNE_VISITOR_ART_AND_GLOW_SAVED");
    }
    static Sprite Portrait(string name)
    {
        string asset=Art+name+"_Portrait.asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(existing!=null)return existing;
        string source=Art+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(source);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.isReadable=true;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(source);var pixels=texture.GetPixels32();int left=texture.width,bottom=texture.height,right=0,top=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>20){left=Mathf.Min(left,x);bottom=Mathf.Min(bottom,y);right=Mathf.Max(right,x);top=Mathf.Max(top,y);}
        if(right<left)throw new System.InvalidOperationException("Empty portrait "+name);
        var sprite=Sprite.Create(texture,new Rect(left,bottom,right-left+1,top-bottom+1),new Vector2(.5f,0),100);sprite.name=name;AssetDatabase.CreateAsset(sprite,asset);return sprite;
    }
}

