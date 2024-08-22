using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UI;


[Serializable]
public class BaseComponentProperty
{
    public string type;
    public string guid;
    [OdinSerialize, HideInInspector]
    public Component target;

    public virtual void PrevProcessing()
    {
        
    }

    public virtual JObject BakeComponent()
    {
        JObject json = new JObject();
        json["type"] = type;
        json["guid"] = guid;
        return json;
    }

    public static BaseComponentProperty CreateProperty(Component component)
    {
        BaseComponentProperty property = null;
        if(component.GetType().Name == "Transform")
        {
            var transformProperty = new TransformProperty();

            property = transformProperty;
        }
        if (component.GetType().Name == "RectTransform")
        {
            var transformProperty = new UITransformProperty();

            property = transformProperty;
        }
        if (component.GetType().Name == "Camera")
        {
            var cameraProperty = new CameraProperty();

            property = cameraProperty;
        }
        if (component.GetType().Name == "Light")
        {
            var lightProperty = new LightProperty();

            property = lightProperty;
        }
        if (component.GetType().Name == "MeshRenderer")
        {
            var meshRendererProperty = new MeshRendererProperty();

            property = meshRendererProperty;
        }
        if (component.GetType().Name == "MeshFilter")
        {
            var meshFilterProperty = new MeshFilterProperty();

            property = meshFilterProperty;
        }
        if (component.GetType().Name == "SkinnedMeshRenderer")
        {
            var skinnedMeshRendererProperty = new SkinnedMeshRendererProperty();

            property = skinnedMeshRendererProperty;
        }

        //Debug.Log(component.GetType().Name);

        if (property != null)
        {
            property.target = component;
            property.type = component.GetType().Name;
            if(string.IsNullOrEmpty(property.guid))
                property.guid = System.Guid.NewGuid().ToString();
        }
        return property;
    }
}

[Serializable]
public class TransformProperty : BaseComponentProperty
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var trans = (Transform)target;

        json["position"] = BakeExtensions.ToJson(trans.localPosition);
        json["rotation"] = BakeExtensions.ToJson(trans.localEulerAngles);
        json["scale"] = BakeExtensions.ToJson(trans.localScale);

        return json;
    }
}


[Serializable]
public class UITransformProperty : BaseComponentProperty
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var trans = (RectTransform)target;


        json["position"] = BakeExtensions.ToJson(trans.localPosition);
        json["rotation"] = BakeExtensions.ToJson(trans.localEulerAngles);
        json["scale"] = BakeExtensions.ToJson(trans.localScale);
        json["pivot"] = BakeExtensions.ToJson(trans.pivot);
        json["anchorMax"] = BakeExtensions.ToJson(trans.anchorMax);
        json["anchorMin"] = BakeExtensions.ToJson(trans.anchorMin);

        return json;
    }
}

[Serializable]
public class MeshRendererProperty : BaseComponentProperty
{
    public override void PrevProcessing()
    {
        base.PrevProcessing();
        var obj = (MeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        SceneBaking.refList_Material.AddRange(materialList);
    }

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();

        var obj = (MeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        var materials = new JArray();

        json.Add("shadowCast", obj.shadowCastingMode.ToString());

        json.Add("materials", materials);
        foreach (var material in materialList){
            materials.Add(SceneBaking.hash2guidTable[material.GetHashCode()]);
        }

        return json;
    }
}

[Serializable]
public class SkinnedMeshRendererProperty : BaseComponentProperty
{
    public override void PrevProcessing()
    {
        base.PrevProcessing();
        var obj = (SkinnedMeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        SceneBaking.refList_Material.AddRange(materialList);
    }
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        

        var obj = (SkinnedMeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        var materials = new JArray();

        json.Add("shadowCast", obj.shadowCastingMode.ToString());

        json.Add("mesh", BakeExtensions.ToJson(obj.sharedMesh));

        json.Add("materials", materials);
        foreach (var material in materialList) {
            materials.Add(SceneBaking.hash2guidTable[material.GetHashCode()]);
        }

        JObject blendShapes = new JObject();
        json.Add("blendShapeCount", obj.sharedMesh.blendShapeCount);
        json.Add("blendShapes", blendShapes);
        for (int i = 0; i < obj.sharedMesh.blendShapeCount; i++) {
            blendShapes.Add(obj.sharedMesh.GetBlendShapeName(i),obj.GetBlendShapeWeight(i));
        }
        return json;
    }
}

[Serializable]
public class MeshFilterProperty : BaseComponentProperty
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (MeshFilter)target;
        json.Add("mesh", BakeExtensions.ToJson(obj.sharedMesh));

        return json;
    }
}

[Serializable]
public class LightProperty : BaseComponentProperty
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (Light)target;
        json.Add("lightType", obj.type.ToString());
        json.Add("color", BakeExtensions.ToJson(obj.color));
        json.Add("intensity", obj.intensity);
        json.Add("range", obj.range);
        json.Add("innerSpotAngle", obj.innerSpotAngle);
        json.Add("spotAngle", obj.spotAngle);
        json.Add("shadowAngle", obj.shadowAngle);
        return json;
    }
}


[Serializable]
public class CameraProperty : BaseComponentProperty
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (Camera)target;
        json.Add("isOrtho", obj.orthographic);
        json.Add("orthoSize", obj.orthographicSize);
        json.Add("near", obj.nearClipPlane);
        json.Add("far", obj.farClipPlane);
        json.Add("fovy", obj.fieldOfView);

        return json;
    }
}
