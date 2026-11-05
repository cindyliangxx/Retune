using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTuneAlbumAuthoring
{
    static ReTuneAlbumAuthoring(){EditorApplication.delayCall+=Apply;}
    [MenuItem("ReTune/Scenes/Connect Interactive Album")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string imagePath="Assets/Resources/StoryArt/AlbumSpread.jpg";
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath)==null)return;
        const string spritePath="Assets/Resources/StoryArt/AlbumSpread_Crop.asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if(sprite==null)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            sprite=Sprite.Create(texture,new Rect(texture.width*104f/1818f,texture.height*120f/1168f,texture.width*1655f/1818f,texture.height*936f/1168f),new Vector2(.5f,.5f),100);sprite.name="Album Spread";AssetDatabase.CreateAsset(sprite,spritePath);
        }
        var book=ReTuneNarrativeBook.Load();var story=AssetDatabase.LoadAssetAtPath<ReTuneStoryData>("Assets/ReTuneScenes/Data/Story.asset");
        story.customers[2].dialogue=book.customers[2].dialogue;story.customers[2].reply=string.Join("\n\n",book.customers[2].reply);EditorUtility.SetDirty(story);
        const string path="Assets/ReTuneScenes/Scenes/04_Reply.unity";var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
        if(!opened&&scene.isDirty){Debug.LogWarning("Save 04_Reply before connecting the album.");return;}
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var flow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneNarrativePlayer>(true)).First();
            var album=flow.GetComponent<ReTuneAlbumSequence>();if(album==null)album=flow.gameObject.AddComponent<ReTuneAlbumSequence>();
            if(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/StoryArt/Album/AlbumPage.png")!=null)
            {
                album.albumSpread=ImportLayer("AlbumPage.png");
                album.photoLayers=new[]{ImportLayer("GuitaristsPhoto.png"),ImportLayer("PostmanPhoto.png")};
                album.photoCloseups=new[]{Closeup("GuitaristsPhoto",new Rect(250,180,535,610)),Closeup("PostmanPhoto",new Rect(1030,415,645,370))};
            }
            else album.albumSpread=sprite;
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("RETUNE_INTERACTIVE_ALBUM_SAVED");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
    static Sprite ImportLayer(string name)
    {
        string path="Assets/Resources/StoryArt/Album/"+name;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer.textureType!=TextureImporterType.Sprite||!importer.isReadable||importer.maxTextureSize!=4096)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.isReadable=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite Closeup(string name,Rect bounds)
    {
        string path="Assets/Resources/StoryArt/Album/"+name+"_Closeup.asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
        var texture=ImportLayer(name+".png").texture;
        var scaled=new Rect(bounds.x*texture.width/1920f,bounds.y*texture.height/1080f,bounds.width*texture.width/1920f,bounds.height*texture.height/1080f);
        sprite=Sprite.Create(texture,scaled,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name+" Closeup";AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
}
