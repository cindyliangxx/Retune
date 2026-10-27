using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ReTune.Scenes;

public static class ReTuneSceneBuilder
{
    const string Root = "Assets/ReTuneScenes";
    static Font font;
    static Sprite circle, ring, square, sector;
    static ReTuneStoryData data;
    static readonly Color Bg = new Color(.065f,.09f,.10f), Panel = new Color(.13f,.18f,.18f), Ink = new Color(.91f,.88f,.79f), Muted = new Color(.60f,.67f,.65f), Gold = new Color(.77f,.58f,.31f), Green = new Color(.30f,.51f,.39f);
    static ReTuneSceneController c;
    static Transform canvas;
    [MenuItem("ReTune/Scenes/Open Start")]
    public static void OpenStart() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Menu)); }
    [MenuItem("ReTune/Scenes/Create Missing Scenes (preserve existing)")]
    public static void Generate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var folder in new[] { "Scenes", "Data", "UI", "Audio" }) Directory.CreateDirectory(Root + "/" + folder);
        font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/ReTuneChinese.ttf");
        if (font == null) throw new Exception("Missing scene font");
        circle = Shape("Circle", false); ring = Shape("Ring", true); square = Shape("Square", false); sector = Shape("StableSector", false);
        data = AssetDatabase.LoadAssetAtPath<ReTuneStoryData>(Root + "/Data/Story.asset");
        if (data == null) { data = ScriptableObject.CreateInstance<ReTuneStoryData>(); PopulateData(); AssetDatabase.CreateAsset(data, Root + "/Data/Story.asset"); }
        MakeAudio();
        foreach (ReTuneSceneController.Screen screen in Enum.GetValues(typeof(ReTuneSceneController.Screen)))
        {
            string path = ReTuneSceneController.ScenePath(screen);
            if (File.Exists(path)) continue; // User edits are authoritative.
            CreateScene(screen); EditorSceneManager.SaveScene(c.gameObject.scene, path);
        }
        RegisterScenes(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Menu));
        Debug.Log("RETUNE_SCENES_GENERATED: 6 authored world and HUD layouts; existing scenes preserved");
    }
    static void PopulateData()
    {
        string[] names = { "Young Postman", "Street Guitarist", "Bookshop Owner", "Old Photographer" };
        string[] emotions = { "Anxiety to Calm", "Anger to Understanding", "Loss to Acceptance", "Longing to Hope" };
        string[][] lines = {
            new[] { "I delivered my first letter to the wrong address.", "Now I check every address again and again.", "I know it is right. I still cannot let go." },
            new[] { "I argued with my longtime music partner.", "He wants to improvise. I want every note to be right.", "We used to play so well outside the bookshop." },
            new[] { "After twenty years, my bookshop is closing.", "I am afraid its stories will be forgotten.", "Someone sent me photos of our street. Perhaps I could make a small show." },
            new[] { "A friend and I once planned to travel and fill a photo album.", "Our letters grew rare. The album is still unfinished.", "I took the photos at the bookshop. Yet I never sent my own letter." }
        };
        string[] replies = { "I will still check the address. Maybe once is enough.\nOutside, a photographer captures him pausing by the bookshop.", "I want to hear our old recordings again.\nHe sends a message: Shall we try your idea next time? No reply yet.", "It still hurts to close. But I can keep these stories.\nTwo photos: the musicians and the postman. The same photographer signed both.", "The album can wait. I can send this letter today.\nHe writes his old friend's address and walks to the postbox." };
        data.customers = new ReTuneStoryData.Customer[4];
        for (int i = 0; i < 4; i++) data.customers[i] = new ReTuneStoryData.Customer { name = names[i], emotion = emotions[i], dialogue = lines[i], reply = replies[i], structure = i, tone = 4 + i % 2, drift = i == 2 ? .19f : .07f, wave = i == 2 ? .1f : .22f, frequency = i == 2 ? .3f : .9f, impulse = i == 1 ? .8f : i == 3 ? .45f : 0, mostlyHold = i == 2 };
        string[] partNames = { "Steady Wheel", "Sync Wheel", "Sustain Spring", "Echo Wheel", "Soft Plate", "Warm Plate" };
        string[] descriptions = { "Steadies a rushed or drifting beat.", "Brings clashing rhythms together.", "Connects broken phrases and holds notes.", "Adds a gentle reply to a lone melody.", "Softens sharp sounds.", "Adds warmth and depth." };
        data.parts = new ReTuneStoryData.Part[6];
        for (int i = 0; i < 6; i++) data.parts[i] = new ReTuneStoryData.Part { name = partNames[i], structure = i < 4, description = descriptions[i] };
    }
    static void CreateScene(ReTuneSceneController.Screen screen)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
        var cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Bg; camera.transform.position = new Vector3(0,0,-10);
        var root = new GameObject("Scene Controller", typeof(ReTuneSceneController)); c = root.GetComponent<ReTuneSceneController>(); c.screen = screen; c.story = data;
        var ui = new GameObject("Canvas_Editable UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
        canvas = ui.transform;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        for (int i = 0; i < 3; i++)
        {
            var sourceObject = new GameObject(new[] { "A_Melody", "B_Reply", "C_Noise" }[i]); sourceObject.transform.SetParent(root.transform);
            var audio = sourceObject.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.loop = true; audio.volume = 0;
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Placeholder_" + i + ".wav");
            if (i == 0) c.trackA = audio; else if (i == 1) c.trackB = audio; else c.trackC = audio;
        }
        ImageBox(canvas,"Background",0,0,1280,720,Bg);
        Txt(canvas,"Game Title","Re:Tune",40,18,590,45,29,Ink);
        c.customerLabel = Txt(canvas,"Visitor and Day","DAY 01   Young Postman / Anxiety to Calm",710,25,530,35,19,Muted);
        if (screen == ReTuneSceneController.Screen.Menu) Menu();
        else if (screen == ReTuneSceneController.Screen.Shop || screen == ReTuneSceneController.Screen.Reply) Shop(screen == ReTuneSceneController.Screen.Reply);
        else if (screen == ReTuneSceneController.Screen.Parts) Parts();
        else if (screen == ReTuneSceneController.Screen.Performance) Performance();
        else Ending();
        c.hint = Txt(canvas,"Status Hint","",40,656,790,58,18,Muted);
        if (screen != ReTuneSceneController.Screen.Menu && screen != ReTuneSceneController.Screen.Ending)
        {
            Btn(canvas,"Retry Button","Retry",865,663,170,38,c.RestartPhase);
            Btn(canvas,"Main Menu Button","Main Menu",1055,663,180,38,c.MainMenu);
        }
        Overlay();
        ReTuneWorldLayout.Convert(c);
    }
    static void Menu()
    {
        ShopBackdrop();
        var box = ImageBox(canvas,"Start Panel",80,200,620,325,Panel).transform;
        Txt(box,"Intro Title","A small shop. A place to listen.",30,30,550,75,30,Ink);
        Txt(box,"Intro Text","Listen. Tune. Play.\nHelp four visitors find their own answers.",30,115,550,90,21,Muted);
        Btn(box,"Start","Start",30,233,260,55,c.StartHealing);
    }
    static void ShopBackdrop()
    {
        var backdrop = ImageBox(canvas,"Shop Background",40,85,1200,550,new Color(.18f,.23f,.22f)).transform;
        var window = ImageBox(backdrop,"Street Window",30,27,560,293,new Color(.29f,.37f,.35f)).transform;
        for (int i = 0; i < 3; i++) ImageBox(window,"Glass_"+i,15+i*180,13,165,220,new Color(.37f,.44f,.40f));
        Txt(window,"Street Label","Old Street / Bookshop",35,38,460,60,23,Muted);
        for (int row = 0; row < 2; row++)
        {
            ImageBox(backdrop,"Shelf_"+row,820,70+row*115,330,12,Gold);
            for (int i = 0; i < 4; i++) ImageBox(backdrop,"Shelf Item_"+row+"_"+i,830+i*80,11+row*115,60,57,Panel);
        }
        Txt(backdrop,"Shelf Label","Radio / Records",846,255,310,35,19,Muted);
        ImageBox(backdrop,"Counter",0,320,1200,30,new Color(.30f,.24f,.18f));
        var box = ImageBox(backdrop,"Music Box",790,340,370,110,new Color(.37f,.28f,.19f)).transform;
        ImageBox(backdrop,"Box Lid",825,250,300,90,new Color(.24f,.20f,.16f));
        Txt(backdrop,"Music Box Label","Music Box",861,275,250,50,22,Ink);
        Txt(box,"Box Decoration","♪",148,12,90,80,55,Gold);
    }
    static void Shop(bool reply)
    {
        ShopBackdrop();
        // All four portraits exist before Play; only their visibility changes with the customer.
        c.portraits = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            var portrait = Rect(canvas,"Portrait_" + data.customers[i].name,330,200,190,225); c.portraits[i] = portrait.gameObject;
            ImageBox(portrait,"Head",30,0,128,128,new Color(.52f-i*.04f,.61f-i*.03f,.50f),circle);
            ImageBox(portrait,"Body",7,125,176,100,new Color(.35f,.47f,.39f));
            Txt(portrait,"Visitor Name",data.customers[i].name,10,150,175,48,18,Ink); portrait.gameObject.SetActive(i == 0);
        }
        var box = ImageBox(canvas,"Dialogue Panel",55,440,740,195,Panel).transform;
        Txt(box,"Speaker Label","Visitor",23,11,660,32,22,Gold);
        c.body = Txt(box,"Dialogue Text",reply ? data.customers[0].reply : data.customers[0].dialogue[0],23,52,685,135,23,Ink);
        c.actionLabel = Btn(canvas,"Next Button",reply ? "Next Day" : "Next",845,575,345,52,c.ContinueStory).GetComponentInChildren<Text>();
    }
    static void Workbench()
    {
        ImageBox(canvas,"Open Music Box",250,90,980,555,new Color(.22f,.24f,.20f));
        var sidebar = ImageBox(canvas,"Visitor Panel",40,90,190,555,Panel).transform;
        Txt(sidebar,"Visitor Title","Visitor",15,16,170,40,24,Gold);
        Txt(sidebar,"Clue Hint","Read the clues.\nPick one rhythm\nand one tone.",15,90,160,120,19,Ink);
        Btn(sidebar,"Dialogue","Dialogue",15,303,160,43,c.ShowHistory);
        c.mixLabel = Txt(sidebar,"Track Status","A Melody\nB Reply\nC Noise",15,424,160,115,17,Muted);
        // Recognizable mechanical placeholders, all saved in the scene hierarchy.
        var mechanics = Rect(canvas,"Gears and Spring",270,560,380,70);
        ImageBox(mechanics,"Gear One",5,7,55,55,Muted,ring);
        ImageBox(mechanics,"Gear Two",60,24,37,37,Gold,ring);
        ImageBox(mechanics,"Spring Shaft",105,24,110,12,Gold);
        for (int i = 0; i < 8; i++) ImageBox(mechanics,"Comb_"+i,260+i*11,6,5,45-i%3*7,Muted);
    }
    static void Parts()
    {
        Workbench();
        Txt(canvas,"Parts Title","Choose Parts",285,105,700,45,26,Ink);
        var structure = Btn(canvas,"Rhythm Slot_Click to Remove","1. Rhythm Slot\nDrop a rhythm part here",295,193,300,88,c.ClearStructure);
        c.structureLabel = structure.GetComponentInChildren<Text>();
        var tone = Btn(canvas,"Tone Slot_Click to Remove","2. Tone Slot\nDrop a tone part here",295,300,300,88,c.ClearTone);
        c.toneLabel = tone.GetComponentInChildren<Text>();
        Txt(canvas,"Slot Hint","Add one rhythm and one tone part.\nClick a slot to remove its part.",295,410,320,100,21,Muted);
        for (int i = 0; i < 6; i++)
        {
            var part = ImageBox(canvas,"Part_" + data.parts[i].name,680+i%2*250,180+i/2*85,230,65,i<4?new Color(.34f,.39f,.33f):new Color(.43f,.32f,.26f));
            part.raycastTarget = true;
            Txt(part.transform,"Name and Type",data.parts[i].name + "\n"+(i<4?"Rhythm / Drag":"Tone / Drag"),12,5,210,55,20,Ink);
            part.gameObject.AddComponent<CanvasGroup>(); var drag = part.gameObject.AddComponent<ReTunePartDrag>(); drag.controller = c; drag.part = i;
        }
        Txt(canvas,"Part Hint","Drag to fit. Double-click for details.",680,455,500,65,20,Muted);
        c.originalButton = Btn(canvas,"Listen Button","Listen",295,515,300,44,c.OriginalMelody);
        c.trialButton = Btn(canvas,"Test","Test",680,565,230,50,c.Trial);
        Btn(canvas,"Clear","Clear",930,565,230,50,c.ClearParts);
    }
    static void Performance()
    {
        Workbench();
        Txt(canvas,"Play Title","Play Together",285,105,700,45,26,Ink);
        Txt(canvas,"Controls Hint","A/D + Mouse",290,158,370,45,22,Ink);
        c.stats = Txt(canvas,"Balance and Hits","Balance 100%\nHits 0 / 8",300,215,340,85,23,Ink);
        Wave("A_Melody Track",690,183,"A Melody",1);
        Wave("B_Reply Track",690,330,"B Reply",.4f);
        c.responseFill = Bar(canvas,"B Reply Level",710,430,445,9,0);
        var dial = ImageBox(canvas,"Balance Dial",345,340,220,220,Panel,circle).transform;
        ImageBox(dial,"Dial Ring",0,0,220,220,Muted,ring);
        ImageBox(dial,"Green Zone_18 Degrees",0,0,220,220,Green,sector);
        var needle = Rect(dial,"Needle Pivot",110,110,6,94); needle.pivot = new Vector2(.5f,0); needle.anchoredPosition = new Vector2(110,-110); c.needle = needle;
        c.needle.gameObject.AddComponent<Image>().color = Gold;
        ImageBox(dial,"Needle Hub",102,102,16,16,Ink,circle);
        Txt(canvas,"A Key Label","A",295,430,60,45,30,Ink); Txt(canvas,"D Key Label","D",575,430,60,45,30,Ink);
        Txt(canvas,"Green Zone Hint","Stay in green",350,324,260,16,14,Muted);
        c.stabilityFill = Bar(canvas,"Balance Bar",300,310,300,12,1);
        Txt(canvas,"Timing Hint","Click or hold when the rings meet.",700,475,480,30,19,Muted);
        var hit = ImageBox(canvas,"Mouse Hit Area",1000,518,170,124,Panel); hit.raycastTarget = true;
        var input = hit.gameObject.AddComponent<ReTuneNoteInput>(); input.controller = c;
        ImageBox(hit.transform,"Hit Center",68,30,34,34,Green,circle);
        var approach = ImageBox(hit.transform,"Timing Ring",62,24,46,46,Gold,ring).rectTransform;
        approach.pivot = new Vector2(.5f,.5f); approach.anchoredPosition = new Vector2(85,-47); c.approachRing = approach;
        c.noteLabel = Txt(hit.transform,"Note Type","Click\nNote 1 / 8",5,80,160,42,17,Ink);
        c.holdFill = Bar(canvas,"Hold Bar",700,550,260,22,0);
        c.phraseFill = Bar(canvas,"Song Bar",700,590,260,12,0);
        Txt(canvas,"Progress Hint","Hold / Song",700,610,275,25,16,Muted);
    }
    static void Wave(string name,float x,float y,string title,float amount)
    {
        var track = ImageBox(canvas,name,x,y,490,121,Panel).transform;
        Txt(track,"Track Title",title,15,8,460,35,20,Ink);
        for (int i = 0; i < 78; i++)
        { float height = 7+Mathf.Abs(Mathf.Sin(i*.35f)*Mathf.Sin(i*.13f))*42*amount; ImageBox(track,"Wave Bar_"+i,15+i*5.8f,80-height/2,4,height,Gold); }
    }
    static void Ending()
    {
        Txt(canvas,"Epilogue Title","A letter finds its way home.",140,180,1000,80,38,Ink);
        ImageBox(canvas,"Letter",140,290,230,220,new Color(.38f,.34f,.25f));
        Txt(canvas,"Letter Text","To an old friend\n\nThe photographer",165,320,190,155,25,Ink);
        c.body = Txt(canvas,"Epilogue Text","The photographer sends his letter.\nThe postman checks the address and delivers it.\n\nOld feelings can make room for new hope.",410,300,760,215,26,Muted);
        Btn(canvas,"Return Button","Main Menu",850,560,320,55,c.MainMenu);
    }
    static void Overlay()
    {
        var shade = ImageBox(canvas,"Help Overlay_Hidden",0,0,1280,720,new Color(0,0,0,.8f)); shade.raycastTarget = true; c.overlay = shade.gameObject;
        var box = ImageBox(shade.transform,"Overlay Panel",225,155,830,420,Panel).transform;
        c.overlayTitle = Txt(box,"Overlay Title","Help / Dialogue",35,30,750,45,28,Gold);
        c.overlayBody = Txt(box,"Overlay Text","Select Continue to return.",35,100,750,210,24,Ink);
        Btn(box,"Continue","Continue",535,340,250,50,c.CloseOverlay);
        shade.gameObject.SetActive(false);
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var go = new GameObject(name,typeof(RectTransform)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent,false);
        rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); rt.anchoredPosition = new Vector2(x,-y); rt.sizeDelta = new Vector2(w,h); return rt;
    }
    static Image ImageBox(Transform parent,string name,float x,float y,float w,float h,Color color,Sprite sprite = null)
    { var rt=Rect(parent,name,x,y,w,h); var image=rt.gameObject.AddComponent<Image>(); image.color=color; image.sprite=sprite; image.raycastTarget=false; return image; }
    static Text Txt(Transform parent,string name,string text,float x,float y,float w,float h,int size,Color color)
    { var rt=Rect(parent,name,x,y,w,h); var label=rt.gameObject.AddComponent<Text>(); label.font=font; label.fontSize=size; label.text=text; label.color=color; label.raycastTarget=false; label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Truncate; return label; }
    static Button Btn(Transform parent,string name,string text,float x,float y,float w,float h,UnityAction action)
    {
        var image=ImageBox(parent,name,x,y,w,h,Panel); image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.25f,1.2f,1.1f); colors.pressedColor=Gold; button.colors=colors;
        var label=Txt(image.transform,"Label",text,8,4,w-16,h-8,20,Ink); label.alignment=TextAnchor.MiddleCenter;
        UnityEventTools.AddPersistentListener(button.onClick,action); return button;
    }
    static Image Bar(Transform parent,string name,float x,float y,float w,float h,float fill)
    { var root=ImageBox(parent,name,x,y,w,h,Panel); var foreground=ImageBox(root.transform,"Fill",0,0,w,h,Gold,square); foreground.type=Image.Type.Filled; foreground.fillMethod=Image.FillMethod.Horizontal; foreground.fillAmount=fill; return foreground; }
    static Sprite Shape(string name,bool outline)
    {
        string path=Root+"/UI/"+name+".png";
        if (!File.Exists(path))
        {
            const int size=128; var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            { float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(64,64)); float alpha=name=="Square"?1:Mathf.Clamp01(63-d); if(outline) alpha*=Mathf.Clamp01(d-57); if(name=="StableSector" && Mathf.Abs(Mathf.Atan2(x+.5f-64,y+.5f-64)*Mathf.Rad2Deg)>18) alpha=0; texture.SetPixel(x,y,new Color(1,1,1,alpha)); }
            texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.mipmapEnabled=false;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void MakeAudio()
    {
        const int rate=22050, length=rate*8;
        for(int layer=0;layer<3;layer++)
        {
            string path=Root+"/Audio/Placeholder_"+layer+".wav"; if(File.Exists(path)) continue;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+length*2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(length*2);
                for(int i=0;i<length;i++) { float t=i/(float)rate; float hz=layer==0?330+((int)(t*2)%4)*55:layer==1?220:913; float sample=Mathf.Sin(t*hz*Mathf.PI*2)*Mathf.Exp(-(t%.5f)*(layer==2?30:9))*.28f; writer.Write((short)(sample*short.MaxValue)); }
            }
            AssetDatabase.ImportAsset(path);
        }
    }
    static void RegisterScenes()
    {
        var existing=EditorBuildSettings.scenes.ToList();
        string start=ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Menu);
        existing.RemoveAll(s=>s.path==start);
        existing.Insert(0,new EditorBuildSettingsScene(start,true));
        foreach(ReTuneSceneController.Screen screen in Enum.GetValues(typeof(ReTuneSceneController.Screen)))
        { string path=ReTuneSceneController.ScenePath(screen); if(!existing.Any(s=>s.path==path)) existing.Add(new EditorBuildSettingsScene(path,true)); }
        EditorBuildSettings.scenes=existing.ToArray();
    }
}
