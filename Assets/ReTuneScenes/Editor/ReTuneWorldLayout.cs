using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ReTune.Scenes;

// Editor-only authoring conversion. Runtime never builds the world or HUD.
public static class ReTuneWorldLayout
{
    static int order;
    static Sprite square;
    static Material material;
    static ReTuneSceneController controller;
    static readonly HashSet<string> WorldRoots = new HashSet<string> {
        "Background", "Shop Background", "Open Music Box", "Gears and Spring",
        "Rhythm Slot_Click to Remove", "Tone Slot_Click to Remove", "Balance Dial",
        "Mouse Hit Area", "A_Melody Track", "B_Reply Track", "Letter"
    };
    public static void Convert(ReTuneSceneController target)
    {
        controller = target;
        var roots = target.gameObject.scene.GetRootGameObjects();
        if (roots.Any(go => go.name == "World")) return;
        var canvas = roots.Select(go => go.GetComponent<Canvas>()).First(value => value != null);
        canvas.name = "UI_HUD";
        foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
        var camera = roots.Select(go => go.GetComponent<Camera>()).First(value => value != null);
        controller.worldCamera = camera;
        camera.orthographicSize = 3.6f;
        if (camera.GetComponent<Physics2DRaycaster>() == null) camera.gameObject.AddComponent<Physics2DRaycaster>();
        var framing = camera.gameObject.AddComponent<ReTuneWorldCamera>(); framing.hudScaler = canvas.GetComponent<CanvasScaler>();
        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ReTuneScenes/UI/Square.png");
        material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        var world = new GameObject("World").transform;
        order = 0;
        foreach (var child in canvas.transform.Cast<Transform>().ToArray())
        {
            if (WorldRoots.Contains(child.name) || child.name.StartsWith("Portrait_") || child.name.StartsWith("Part_"))
                ConvertNode((RectTransform)child, world, new Vector2(1280,720), new Vector2(.5f,.5f));
        }
        controller.partObjects = world.GetComponentsInChildren<ReTunePartDrag>(true).OrderBy(part => part.part).ToArray();
    }
    static Transform ConvertNode(RectTransform old, Transform parent, Vector2 parentSize, Vector2 parentPivot)
    {
        Vector2 size = old.sizeDelta + Vector2.Scale(old.anchorMax - old.anchorMin, parentSize);
        Vector2 position = Vector2.Scale(old.anchorMin - parentPivot, parentSize) + old.anchoredPosition;
        Vector2 pivot = old.pivot;
        Quaternion rotation = old.localRotation;
        int sorting = ++order * 10;
        var text = old.GetComponent<Text>();
        if (text != null)
        {
            old.SetParent(parent,false); old.anchorMin = old.anchorMax = new Vector2(.5f,.5f);
            old.sizeDelta = size; old.localScale = Vector3.one * .01f; old.localPosition = (Vector3)position * .01f; old.localRotation = rotation;
            var labels = old.gameObject.AddComponent<Canvas>(); labels.renderMode = RenderMode.WorldSpace; labels.overrideSorting = true; labels.sortingOrder = sorting;
            text.raycastTarget = false; old.gameObject.layer = 0;
            return old;
        }
        var go = new GameObject(old.name); go.layer = 0; go.SetActive(old.gameObject.activeSelf);
        var node = go.transform; node.SetParent(parent,false); node.localPosition = (Vector3)position * .01f; node.localRotation = rotation;
        var image = old.GetComponent<Image>();
        Vector2 offset = Vector2.Scale(new Vector2(.5f,.5f) - pivot, size) * .01f;
        GameObject visual = null;
        if (image != null)
        {
            visual = new GameObject("Visual",typeof(SpriteRenderer)); visual.transform.SetParent(node,false); visual.transform.localPosition = offset;
            var renderer = visual.GetComponent<SpriteRenderer>(); renderer.sprite = image.sprite != null ? image.sprite : square;
            renderer.color = image.color; renderer.sharedMaterial = material; renderer.drawMode = SpriteDrawMode.Sliced; renderer.size = size * .01f; renderer.sortingOrder = sorting;
        }
        foreach (var child in old.Cast<Transform>().ToArray()) ConvertNode((RectTransform)child,node,size,pivot);
        bool isPart = old.GetComponent<ReTunePartDrag>() != null;
        bool isNote = old.GetComponent<ReTuneNoteInput>() != null;
        bool isSlot = old.name == "Rhythm Slot_Click to Remove" || old.name == "Tone Slot_Click to Remove";
        bool isBox = old.name == "Music Box" && controller.screen == ReTuneSceneController.Screen.Shop;
        if (isPart || isNote || isSlot || isBox)
        {
            var collider = visual.AddComponent<BoxCollider2D>(); collider.size = size * .01f;
            if (isPart) { var drag = go.AddComponent<ReTunePartDrag>(); drag.controller = controller; drag.part = old.GetComponent<ReTunePartDrag>().part; }
            if (isNote) go.AddComponent<ReTuneNoteInput>().controller = controller;
            if (isSlot || isBox)
            {
                var click = go.AddComponent<ReTuneWorldClick>(); click.controller = controller;
                click.action = isBox ? ReTuneWorldClick.Action.TouchMusicBox : old.name.StartsWith("Rhythm") ? ReTuneWorldClick.Action.ClearRhythm : ReTuneWorldClick.Action.ClearTone;
                if (isSlot) { if (click.action == ReTuneWorldClick.Action.ClearRhythm) controller.structureSlot = collider; else controller.toneSlot = collider; }
            }
        }
        if (controller.needle == old) controller.needle = node;
        if (controller.approachRing == old) controller.approachRing = node;
        if (controller.portraits != null) for (int i=0;i<controller.portraits.Length;i++) if (controller.portraits[i] == old.gameObject) controller.portraits[i] = go;
        Object.DestroyImmediate(old.gameObject);
        return node;
    }
}
