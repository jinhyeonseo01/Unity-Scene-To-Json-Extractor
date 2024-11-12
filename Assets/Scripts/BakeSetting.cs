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
using UnityEditor.SearchService;

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
