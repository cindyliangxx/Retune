using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
public static class ReTuneTitleMotionAuthoring
{
    public static void Configure(Image cover,RectTransform logo,RectTransform[] menus)
    {
        const string path="Assets/ReTuneScenes/Art/Title/Layers/";
        string[] names={"主场景1-完整.png","主场景1-后面.png","主场景1-中景.png","主场景1-前景.png"};
        var root=cover.transform.Find("Layered Background");
        if(root==null){var go=new GameObject("Layered Background",typeof(RectTransform));go.layer=5;go.transform.SetParent(cover.transform,false);root=go.transform;}
        var rect=(RectTransform)root;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;root.SetAsFirstSibling();
        var layers=new RectTransform[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path+names[i]);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
            var child=root.Find(names[i]);
            if(child==null){var go=new GameObject(names[i],typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(root,false);child=go.transform;}
            var image=child.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path+names[i]);image.raycastTarget=false;image.preserveAspect=false;
            child.SetSiblingIndex(i);layers[i]=(RectTransform)child;
        }
        cover.color=Color.clear;cover.raycastTarget=false;
        var motion=cover.GetComponent<ReTuneTitleMotion>();if(motion==null)motion=cover.gameObject.AddComponent<ReTuneTitleMotion>();
        motion.artwork=rect;motion.backgroundLayers=layers;motion.logo=logo;motion.menus=menus;
        foreach(var menu in menus)if(menu!=null){var image=menu.GetComponent<Image>();if(image!=null)image.raycastTarget=true;}
        // The component also arranges layers in edit mode; keep everything visible there.
        motion.enabled=false;motion.enabled=true;
    }
}
