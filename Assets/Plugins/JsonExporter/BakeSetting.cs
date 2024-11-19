using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEditor;


public class BakeSetting : MonoBehaviour
{
    [FolderPath]
    public string jsonExportPath = "./Assets/Exports/";
    public string resourcesPath = "Assets/";
    [Button]
    public void PathUpdate()
    {
        BakeUnity.definePath_Resources = resourcesPath;
        BakeUnity.exportPath = jsonExportPath;
    }
}
