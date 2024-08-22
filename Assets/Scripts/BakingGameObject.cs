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


public class BakingGameObject : MonoBehaviour
{
    public string name;
    public string guid;

    [SerializeReference, OdinSerialize]
    public List<BaseComponentProperty> bakeComponentList;
}
