using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Unity.VisualScripting;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using System.IO;


public class SceneBaking : MonoBehaviour
{
    public static string definePath_Resources = "Assets/";
    public static Dictionary<string, string> nameToPath = new Dictionary<string, string>(4092);
    public static Dictionary<int, string> hash2guidTable = new Dictionary<int, string>(4092);

    public static List<GameObject> refList_GameObject;
    public static List<Material> refList_Material;
    public static List<Component> refList_Component;

    //[TextArea(0, 30), SerializeField]
    //protected string finalJson;

    public string exportPath = "./Assets/Exports/";

    [Button]
    private void Baking()
    {
        hash2guidTable ??= new Dictionary<int, string>();

        Scene scene = SceneManager.GetActiveScene();
        var sceneName = scene.name;
        Debug.Log($"Baking Scene : {sceneName}");


        refList_GameObject = GameObject.FindObjectsOfType<GameObject>(true).ToList();
        //refList_GameObject = scene.GetRootGameObjects().ToList();


        refList_Material ??= new List<Material>();
        refList_Material.Clear();

        refList_Component ??= new List<Component>();
        refList_Component.Clear();


        //에디터용 제거
        refList_GameObject = refList_GameObject.Where(e => (!e.name.Contains("#")) && (e != this.gameObject)).ToList();
        Debug.Log($"total GameObject : {refList_GameObject.Count}");
        JArray gameObjects = new JArray();


        JObject totalJson;
        InitJson(out totalJson);

        foreach (var obj in refList_GameObject)
            PrevProcessingGameObject(obj);
        foreach (var obj in refList_Component)
            PrevProcessingComponent(obj);
        foreach (var obj in refList_Material)
            PrevProcessingMaterial(obj);

        foreach (var obj in refList_GameObject)
            BakeGameObject(totalJson, obj);
        foreach (var obj in refList_Material)
            BakeMaterial(totalJson, obj);
        foreach (var obj in refList_GameObject)
            BakeObject(totalJson, obj);

        var finalJson = totalJson.ToSafeString();
        string dirPath = Path.GetDirectoryName(exportPath);
        string filePath = $"{dirPath}/{sceneName}.json";
        // StreamWriter를 사용하여 문자열을 파일에 저장
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine(finalJson);
        }
        AssetDatabase.Refresh();

        Console.WriteLine("파일에 저장되었습니다.");
    }

    [Button]
    private void Save()
    {
        Debug.Log("Save");
    }

    public void InitJson(out JObject obj)
    {
        obj = new JObject();
        obj["references"] ??= new JObject();
        obj["path"] ??= new JObject();
        obj["path"]["resources"] ??= new JArray();
        //obj["references"] ??= new JArray();
    }
    public void PrevProcessingGameObject(GameObject element)
    {
        var bakingInfo = element.GetComponent<BakingGameObject>();
        if(bakingInfo == null) bakingInfo = element.AddComponent<BakingGameObject>();
        if (string.IsNullOrEmpty(bakingInfo.guid))
            bakingInfo.guid = System.Guid.NewGuid().ToString();
        if (string.IsNullOrEmpty(bakingInfo.name))
            bakingInfo.name = element.gameObject.name;


        hash2guidTable[element.gameObject.GetHashCode()] = bakingInfo.guid;

        bakingInfo.bakeComponentList ??= new List<BaseComponentProperty>();
        bakingInfo.bakeComponentList.Clear();

        refList_Component.AddRange(element.gameObject.GetComponents<Component>().ToList());
    }
    public void PrevProcessingComponent(Component component)
    {
        var bakingInfo = component.gameObject.GetComponent<BakingGameObject>();
        var property = BaseComponentProperty.CreateProperty(component);
        if (property != null)
        {
            property.PrevProcessing();
            hash2guidTable[component.GetHashCode()] = property.guid;
            bakingInfo.bakeComponentList.Add(property);
        }
    }
    public void PrevProcessingMaterial(Material material)
    {
        var hash = material.GetHashCode();
        if (!SceneBaking.hash2guidTable.ContainsKey(hash)) {
            SceneBaking.hash2guidTable.Add(hash, System.Guid.NewGuid().ToString());
        }
    }

    public void BakeGameObject(JObject prevJson, GameObject obj)
    {
        //prevJson.Add("type", "GameObject");

        var bakingInfo = obj.GetComponent<BakingGameObject>();

        JObject objJson = new JObject();
        var typeKey = (objJson["type"] = "GameObject").ToString();
        objJson["name"] = bakingInfo.name;
        objJson["guid"] = bakingInfo.guid;

        objJson["components"] ??= new JArray();
        objJson["childs"] ??= new JArray();
        objJson["parent"] = "";

        //----------------------------------------

        if (obj.transform.parent != null)
            objJson["parent"] = hash2guidTable[obj.transform.parent.gameObject.GetHashCode()];

        foreach (var property in bakingInfo.bakeComponentList)
        {
            ((JArray)(objJson["components"])).Add(property.guid);
        }

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            try
            {
                ((JArray)(objJson["childs"])).Add(hash2guidTable[obj.transform.GetChild(i).gameObject.GetHashCode()]);
            }
            catch {
                Debug.LogError(obj.transform.GetChild(i).gameObject.name);
                Debug.LogError(hash2guidTable.ContainsKey(obj.transform.GetChild(i).gameObject.GetHashCode()));
                
            }
        }


        var refJson = prevJson["references"];
        (refJson as JObject)[typeKey] ??= new JArray();
        (refJson[typeKey] as JArray)?.Add(objJson);

        //----------------------------------------

        (refJson as JObject)["Components"] ??= new JArray();
        foreach (var property in bakingInfo.bakeComponentList)
            ((refJson as JObject)["Components"] as JArray).Add(property.BakeComponent());

        //----------------------------------------
        

        //Debug.Log(prevJson.ToString());
    }
    public void BakeComponent(JObject prevJson, Component obj)
    {

    }

    public void BakeMaterial(JObject prevJson, Material obj)
    {
        var refJson = prevJson["references"];
        (refJson as JObject)["Materials"] ??= new JArray();
        ((refJson as JObject)["Materials"] as JArray).Add(BakeExtensions.ToJson(obj));
    }

    public void BakeObject<T>(JObject prevJson, T t) where T : class
    {
        //prevJson.Add("type", "GameObject");
        //JObject objJson = new JObject();
        //Debug.Log(prevJson.ToString());
    }
}
