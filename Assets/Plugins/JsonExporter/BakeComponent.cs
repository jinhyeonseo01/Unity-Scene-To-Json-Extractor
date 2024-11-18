using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sirenix.Serialization;
using UnityEngine;


[Serializable]
public class BaseBakeComponent
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

    public static BaseBakeComponent CreateProperty(Component component)
    {
        BaseBakeComponent property = null;
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

        if (component.GetType().Name == "BoxCollider")
        {
            var boxCollider = new BoxColliderProperty();

            property = boxCollider;
        }
        if (component.GetType().Name == "SphereCollider")
        {
            var sphereCollider = new CapualeColliderProperty();

            property = sphereCollider;
        }

        if (component.GetType().Name == "CapsuleCollider")
        {
            var capsuleCollider = new CapsuleColliderProperty();

            property = capsuleCollider;
        }

        if (component.GetType().Name == "MeshCollider")
        {
            var meshCollider = new MeshColliderProperty();

            property = meshCollider;
        }

        //Debug.Log(component.GetType().Name);

        if (property != null)
        {
            property.target = component;

            if (string.IsNullOrEmpty(property.type))
                property.type = component.GetType().Name;
            if(string.IsNullOrEmpty(property.guid))
                property.guid = BakeUnity.NewGuid();
        }
        return property;
    }
}

[Serializable]
public class TransformProperty : BaseBakeComponent
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var trans = (Transform)target;

        json["position"] = BakeExtensions.ToJson(trans.localPosition);
        json["rotation"] = BakeExtensions.ToJson(trans.localRotation);
        json["scale"] = BakeExtensions.ToJson(trans.localScale);

        return json;
    }
}


[Serializable]
public class UITransformProperty : BaseBakeComponent
{
    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var trans = (RectTransform)target;


        json["position"] = BakeExtensions.ToJson(trans.localPosition);
        json["rotation"] = BakeExtensions.ToJson(trans.localRotation);
        json["scale"] = BakeExtensions.ToJson(trans.localScale);
        json["pivot"] = BakeExtensions.ToJson(trans.pivot);
        json["anchorMax"] = BakeExtensions.ToJson(trans.anchorMax);
        json["anchorMin"] = BakeExtensions.ToJson(trans.anchorMin);

        return json;
    }
}

[Serializable]
public class MeshRendererProperty : BaseBakeComponent
{
    public override void PrevProcessing()
    {
        base.PrevProcessing();
        var obj = (MeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        foreach (var material in materialList)
            if (!BakeUnity.refList_Material.Contains(material))
                BakeUnity.refList_Material.Add(material);
    }

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();

        var obj = (MeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        var materials = new JArray();
        json.Add("mesh", BakeExtensions.ToJson(obj.gameObject.GetComponent<MeshFilter>().sharedMesh));
        json.Add("shadowCast", obj.shadowCastingMode.ToString());

        json.Add("materials", materials);
        foreach (var material in materialList){
            if(BakeUnity.TryGetGuid(material, out var guid))
                materials.Add(guid);
        }

        return json;
    }
}

[Serializable]
public class SkinnedMeshRendererProperty : BaseBakeComponent
{
    public override void PrevProcessing()
    {
        base.PrevProcessing();
        var obj = (SkinnedMeshRenderer)target;
        var materialList = obj.sharedMaterials.ToList();
        foreach (var material in materialList)
            if(!BakeUnity.refList_Material.Contains(material))
                BakeUnity.refList_Material.Add(material);
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
            if (BakeUnity.TryGetGuid(material, out var guid))
                materials.Add(guid);
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
public class MeshFilterProperty : BaseBakeComponent
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
public class LightProperty : BaseBakeComponent
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
public class CameraProperty : BaseBakeComponent
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


[Serializable]
public class ColliderProperty : BaseBakeComponent
{
    public ColliderType colliderType;

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (Collider)target;
        json.Add("aabbCenter", BakeExtensions.ToJson(obj.bounds.center));
        json.Add("aabbExtent", BakeExtensions.ToJson(obj.bounds.extents));

        json.Add("isTrigger", obj.isTrigger);

        return json;
    }
}

[Serializable]
public class BoxColliderProperty : ColliderProperty
{

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (BoxCollider)target;
        json.Add("colliderType", "box");
        json.Add("center", BakeExtensions.ToJson(obj.center));
        json.Add("size", BakeExtensions.ToJson(obj.size)); 

        return json;
    }
}

[Serializable]
public class CapualeColliderProperty : ColliderProperty
{

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (SphereCollider)target;
        //json\
        json.Add("colliderType", "sphere");
        json.Add("center", BakeExtensions.ToJson(obj.center));
        json.Add("radius", obj.radius);

        return json;
    }
}
[Serializable]
public class CapsuleColliderProperty : ColliderProperty
{

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (CapsuleCollider)target;
        //json\
        json.Add("colliderType", "capsule");
        json.Add("center", BakeExtensions.ToJson(obj.center));
        json.Add("radius", obj.radius);
        json.Add("height", obj.height);

        return json;
    }
}


[Serializable]
public class MeshColliderProperty : ColliderProperty
{

    public override JObject BakeComponent()
    {
        JObject json = base.BakeComponent();
        var obj = (MeshCollider)target;
        //json
        json.Add("colliderType", "mesh");
        json.Add("convex", obj.convex);
        json.Add("mesh", BakeExtensions.ToJson(obj.sharedMesh));

        return json;
    }
}


public enum ColliderType
{
    box,
    sphere,
    capsule,
    mesh
}