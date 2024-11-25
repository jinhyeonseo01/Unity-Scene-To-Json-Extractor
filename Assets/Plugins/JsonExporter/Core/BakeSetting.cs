using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Sirenix.OdinInspector;


public class BakeSetting : MonoBehaviour
{
    //[FolderPath]
    [NonSerialized]
    public string jsonExportPath = "./Assets/Exports/";
    public string resourcesPath = "Assets/";
    public void PathUpdate()
    {
        BakeUnity.definePath_Resources = resourcesPath;
        BakeUnity.exportPath = jsonExportPath;
    }
}
