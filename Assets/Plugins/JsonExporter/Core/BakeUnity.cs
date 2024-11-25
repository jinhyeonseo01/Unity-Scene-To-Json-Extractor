using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEditor;
using System.IO;
using Unity.VisualScripting;

[InitializeOnLoad]
public class BakeUnity : MonoBehaviour
{
    //[OdinSerialize]
    public static string definePath_Resources = "Assets/";
    public static Dictionary<string, string> nameToPathTable = new Dictionary<string, string>(4092);
    public static Dictionary<int, string> hashToGuidTable = new Dictionary<int, string>(4092);

    public static List<GameObject> refList_GameObject;
    public static List<Material> refList_Material;
    public static List<Component> refList_Component;

    public static Dictionary<GameObject, BakeGameObject> gameObjectToBakeTable;
    public static Dictionary<Component, BaseBakeComponent> componentToBakeTable;

    [NonSerialized, HideInInspector]
    protected static string finalJson;
    //[OdinSerialize]
    public static string exportPath = "./Assets/Exports/";

    //[Button]
    public static void Baking()
    {
        InitBake();
        JObject totalJson;
        InitJson(out totalJson);

        refList_GameObject = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
            .Where(e => !e.name.Contains("#"))
            .ToList();

        foreach (var gameObject in refList_GameObject)
            PrevProcessingGameObject(gameObject);
        foreach (var component in refList_Component)
            PrevProcessingComponent(component);
        foreach (var material in refList_Material)
            PrevProcessingMaterial(material);

        Debug.Log($"total GameObject : {refList_GameObject.Count}");
        Debug.Log($"total Component : {refList_Component.Count}");
        Debug.Log($"total Material : {refList_Material.Count}");

        foreach (var gameObject in refList_GameObject)
            BakeGameObject(totalJson, gameObject);
        foreach (var component in refList_Component)
            BakeComponent(totalJson, component);
        foreach (var material in refList_Material)
            BakeMaterial(totalJson, material);
        foreach (var obj in refList_GameObject)
            BakeObject(totalJson, obj);

        finalJson = totalJson.ToSafeString();

        Debug.Log($"total Json Line : {finalJson.Count((e) => e == '\n')}");

        UnityEngine.SceneManagement.Scene scene = SceneManager.GetActiveScene();
        Save(finalJson, scene.name);
    }

    //[Button]
    private static void Save(string data, string name)
    {
        var sceneName = name;
        Debug.Log($"Baking Name : {sceneName}");

        string dirPath = exportPath;// Path.GetDirectoryName(exportPath);
        string filePath = $"{dirPath}/{sceneName}.json";
        // StreamWriter를 사용하여 문자열을 파일에 저장
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
        using (StreamWriter writer = new StreamWriter(filePath)) {
            writer.WriteLine(finalJson);
        }
        AssetDatabase.Refresh();

        Console.WriteLine("파일에 저장되었습니다.");
    }

    //[Button]
    public static void SelectBaking()
    {
        InitBake();
        JObject totalJson;
        InitJson(out totalJson);

        refList_GameObject = Selection.gameObjects.ToList().Where(e => !e.name.Contains("#")).ToList();

        if (refList_GameObject.Count == 0)
            return;

        foreach (var gameObject in refList_GameObject)
            PrevProcessingGameObject(gameObject);
        foreach (var component in refList_Component)
            PrevProcessingComponent(component);
        foreach (var material in refList_Material)
            PrevProcessingMaterial(material);

        Debug.Log($"total GameObject : {refList_GameObject.Count}");
        Debug.Log($"total Component : {refList_Component.Count}");
        Debug.Log($"total Material : {refList_Material.Count}");

        foreach (var gameObject in refList_GameObject)
            BakeGameObject(totalJson, gameObject);
        foreach (var component in refList_Component)
            BakeComponent(totalJson, component);
        foreach (var material in refList_Material)
            BakeMaterial(totalJson, material);
        foreach (var obj in refList_GameObject)
            BakeObject(totalJson, obj);

        finalJson = totalJson.ToSafeString();

        Debug.Log($"total Json Line : {finalJson.Count((e) => e == '\n')}");

        //UnityEngine.SceneManagement.Scene scene = SceneManager.GetActiveScene();
        Save(finalJson, $"{Selection.gameObjects[0].name} with {Selection.gameObjects.Length}Count");
    }
    public static void InitBake()
    {
        refList_GameObject ??= new List<GameObject>(8192);
        refList_GameObject.Clear();

        refList_Material ??= new List<Material>(8192);
        refList_Material.Clear();

        refList_Component ??= new List<Component>(8192);
        refList_Component.Clear();

        nameToPathTable = new Dictionary<string, string>(8192);
        nameToPathTable.Clear();

        hashToGuidTable = new Dictionary<int, string>(8192);
        hashToGuidTable.Clear();

        gameObjectToBakeTable ??= new Dictionary<GameObject, BakeGameObject>(8192);
        gameObjectToBakeTable.Clear();

        componentToBakeTable ??= new Dictionary<Component, BaseBakeComponent>(8192);
        componentToBakeTable.Clear();
    }
    public static void InitJson(out JObject json)
    {
        json = new JObject();
        json["references"] ??= new JObject();
        //json["path"] ??= new JObject();
        //json["path"]["resources"] ??= new JArray();

        var refJson = json["references"];
        (refJson as JObject)["GameObjects"] ??= new JArray();
        (refJson as JObject)["Materials"] ??= new JArray();
        (refJson as JObject)["Components"] ??= new JArray();
        //obj["references"] ??= new JArray();
    }
    public static void PrevProcessingGameObject(GameObject gameObject)
    {
        if (!gameObjectToBakeTable.TryGetValue(gameObject, out var bakingInfo))
        {
            bakingInfo = new BakeGameObject(gameObject);
            gameObjectToBakeTable.Add(gameObject, bakingInfo);
        }

        SetGuidAndUpdate(gameObject, bakingInfo.guid);

        refList_Component.AddRange(gameObject.GetComponents<Component>()
            .Where(e => !e.IsUnityNull())
            .ToList());

    }
    public static void PrevProcessingComponent(Component component)
    {
        //gameObjectToBakeTable.TryGetValue(component.gameObject, out var bakingInfo);
        if (!componentToBakeTable.TryGetValue(component, out var property))
        {
            if ((property = BaseBakeComponent.CreateProperty(component)) != null)
                componentToBakeTable.Add(component, property);
        }

        if (componentToBakeTable.TryGetValue(component, out property))
        {
            SetGuidAndUpdate(component, property.guid);
            property.PrevProcessing();
        }

    }
    public static void PrevProcessingMaterial(Material material)
    {
        TrySetGuid(material);
    }

    public static void BakeGameObject(JObject prevJson, GameObject gameObject)
    {
        //prevJson.Add("type", "GameObject");

        if (gameObjectToBakeTable.TryGetValue(gameObject, out var bakingInfo))
        {

            JObject objJson = new JObject();
            objJson["name"] = bakingInfo.name;
            objJson["guid"] = bakingInfo.guid;

            objJson["components"] ??= new JArray();
            objJson["childs"] ??= new JArray();
            objJson["parent"] = "";

            //----------------------------------------

            if (gameObject.transform.parent != null)
                if (TryGetGuid(gameObject.transform.parent.gameObject, out var parentGuid))
                    objJson["parent"] = parentGuid;

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                if (TryGetGuid(gameObject.transform.GetChild(i).gameObject, out var childGuid))
                    ((JArray)(objJson["childs"])).Add(childGuid);
            }

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (TryGetGuid(component, out var guid))
                    ((JArray)(objJson["components"])).Add(guid);
            }


            var typeKey = "GameObjects";
            var refJson = prevJson["references"];
            (refJson as JObject)[typeKey] ??= new JArray();
            ((refJson as JObject)[typeKey] as JArray)?.Add(objJson);

        }
    }
    public static void BakeComponent(JObject prevJson, Component component)
    {
        var refJson = prevJson["references"];
        (refJson as JObject)["Components"] ??= new JArray();
        if (componentToBakeTable.TryGetValue(component, out var property))
            ((refJson as JObject)["Components"] as JArray).Add(property.BakeComponent());
    }
    public static void BakeMaterial(JObject prevJson, Material obj)
    {
        var refJson = prevJson["references"];
        (refJson as JObject)["Materials"] ??= new JArray();
        ((refJson as JObject)["Materials"] as JArray).Add(BakeExtensions.ToJson(obj, false));
    }

    public static void BakeObject<T>(JObject prevJson, T t) where T : class
    {
        //prevJson.Add("type", "GameObject");
        //JObject objJson = new JObject();
        //Debug.Log(prevJson.ToString());
    }
    public static bool TryGetGuid<T>(T obj, out string guid) where T : class
    {
        guid = "null";
        if (obj != null && hashToGuidTable.TryGetValue(obj.GetHashCode(), out guid))
            return true;
        return false;
    }
    public static string TrySetGuid<T>(T obj) where T : class
    {
        if (obj == null)
            return "null";
        if (!hashToGuidTable.TryGetValue(obj.GetHashCode(), out var value))
            return hashToGuidTable[obj.GetHashCode()] = NewGuid();
        return value;
    }
    public static string SetGuidAndUpdate<T>(T obj, string guid) where T : class
    {
        return hashToGuidTable[obj.GetHashCode()] = guid;
    }
    public static string NewGuid()
    {
        return System.Guid.NewGuid().ToString();
    }
}
