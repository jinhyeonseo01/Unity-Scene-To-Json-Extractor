using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Sirenix.Serialization;


public class BakeGameObject
{
    public GameObject target;
    public string name;
    public string guid;

    public BakeGameObject(GameObject target)
    {
        this.target = target;
        if (string.IsNullOrEmpty(guid))
            guid = BakeUnity.NewGuid();
        if (string.IsNullOrEmpty(name))
            name = target.name;
    }
}
