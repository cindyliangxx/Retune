using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ReTune.Scenes;
[InitializeOnLoad]
public static class ReTunePartsAuthoring
{
    static ReTunePartsAuthoring() { EditorApplication.delayCall += AutoApply; }
    static void AutoApply() { if (EditorApplication.isPlayingOrWillChangePlaymode) return; var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); if (scene.path == "Assets/ReTuneScenes/Scenes/02_Parts.unity" && !scene.isDirty && Object.FindObjectOfType<ReTunePartsArt>() == null) Apply(); }
    static SpriteRenderer Art(string file,Vector2 pos,Vector2 size,int order,Transform parent=null)
    {
        string path="Assets/Resources/PartsArt/"+file+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        var go=new GameObject("Art_"+file);go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;
        go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return r;
    }
    static BoxCollider2D Area(string name,Vector2 pos,Vector2 size){var go=new GameObject(name);go.transform.position=pos;var b=go.AddComponent<BoxCollider2D>();b.size=size;return b;}
    [MenuItem("ReTune/Scenes/Apply Illustrated Parts")]
    public static void Apply()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ReTuneScenes/Scenes/02_Parts.unity");
        var c=Object.FindObjectOfType<ReTuneSceneController>();
        if(c.GetComponent<ReTunePartsArt>()!=null)return;
        var art=c.gameObject.AddComponent<ReTunePartsArt>();art.controller=c;
        var world=GameObject.Find("World");
        foreach(var r in world.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
        foreach(var t in world.GetComponentsInChildren<Text>())t.enabled=false;
        var canvas=GameObject.Find("UI_HUD");
        foreach(Transform child in canvas.transform) if(child.GetComponent<Button>()==null && child.gameObject!=c.overlay && child.gameObject!=c.hint.gameObject)child.gameObject.SetActive(false);
        c.worldCamera.backgroundColor=new Color(.65f,.55f,.53f);
        var paper=Art("180645",Vector2.zero,new Vector2(11.9f,6.2f),-20);paper.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ReTuneScenes/UI/Square.png");paper.transform.localScale=new Vector3(11.9f/paper.sprite.bounds.size.x,6.2f/paper.sprite.bounds.size.y,1);paper.color=new Color(.97f,.94f,.87f);
        var blue=Object.Instantiate(paper);blue.name="Blue work surface";blue.transform.localScale=paper.transform.localScale*.85f;blue.color=new Color(.72f,.84f,.87f);blue.sortingOrder=-19;
        Art("180516",new Vector2(-2.5f,-.3f),new Vector2(6.2f,3.9f),1);
        art.gear=Art("173613",new Vector2(-.8f,-1.35f),new Vector2(1.8f,1.8f),5).transform;
        art.small=Art("173854",new Vector2(-1.55f,-.8f),new Vector2(.55f,.55f),4).transform;
        Art("180645",new Vector2(-2.55f,-1.35f),new Vector2(1.65f,2.1f),6);
        Art("182114",new Vector2(-2.55f,-2),new Vector2(1.65f,.8f),9);
        // The supplied layers contain the cylinder supports but no complete cylinder: tint the existing plate as a temporary drum.
        var drum=Art("182150",new Vector2(-3,.5f),new Vector2(2.5f,1.5f),7);art.roller=drum.transform;
        art.rollerArea=Area("Roller hover",new Vector2(-3,.5f),new Vector2(2.5f,1.5f));
        art.windingKey=Art("174101",new Vector2(-.65f,1),new Vector2(1.5f,1.5f),8).transform;
        art.windingArea=Area("Wind to test",new Vector2(-.65f,1),new Vector2(1.5f,1.5f));
        Move(c.structureSlot,new Vector2(-.8f,-1.35f),new Vector2(1.9f,1.9f));Move(c.toneSlot,new Vector2(-3,.5f),new Vector2(2.5f,1.5f));
        string[] files={"174143","174101","174133","174112","182150","182150"};
        foreach(var p in c.partObjects){int i=p.part;p.transform.position=new Vector3(2.05f+i%2*1.85f,1.45f-i/2*1.6f,0);Art(files[i],Vector2.zero,i<4?new Vector2(1.4f,1.4f):new Vector2(1.55f,1.1f),20,p.transform);var b=p.GetComponentInChildren<BoxCollider2D>();b.transform.localPosition=Vector3.zero;b.size=new Vector2(1.5f,1.4f);}
        c.trialButton.gameObject.SetActive(false);
        Position(c.originalButton,"Original Melody",25,100,260,65);
        foreach(var b in canvas.GetComponentsInChildren<Button>()){if(b.name=="Clear")Position(b,"Clear",1020,600,150,50);if(b.name.Contains("History"))Position(b,"Review",25,20,230,65);}
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PARTS_ART_SAVED");
    }
    static void Move(Collider2D slot,Vector2 p,Vector2 size){slot.transform.position=p;slot.offset=Vector2.zero;if(slot is BoxCollider2D b)b.size=size;}
    static void Position(Button b,string text,float x,float y,float w,float h){var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);b.GetComponentInChildren<Text>().text=text;b.GetComponent<Image>().color=new Color(.64f,.53f,.51f);}
}

