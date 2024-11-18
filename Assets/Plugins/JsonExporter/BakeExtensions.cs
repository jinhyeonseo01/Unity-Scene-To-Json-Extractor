using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class BakeExtensions
{
    public static JToken ToJson(object obj, bool changeLink = false)
    {
        if (obj is Matrix4x4)
        {
            var value = (Matrix4x4)obj;
            JArray array = new JArray();
            for (int i = 0; i < 16; i++)
                array.Add(value[i]);
            return array;
        }
        if (obj is Quaternion)
        {
            var value = (Quaternion)obj;
            JArray array = new JArray();
            array.Add(value.x);
            array.Add(value.y);
            array.Add(value.z);
            array.Add(value.w);
            return array;
        }
        if (obj is Vector4)
        {
            var value = (Vector4)obj;
            JArray array = new JArray();
            array.Add(value.x);
            array.Add(value.y);
            array.Add(value.z);
            array.Add(value.w);
            return array;
        }
        if (obj is Vector3)
        {
            var value = (Vector3)obj;
            JArray array = new JArray();
            array.Add(value.x);
            array.Add(value.y);
            array.Add(value.z);
            return array;
        }
        if (obj is Vector2)
        {
            var value = (Vector2)obj;
            JArray array = new JArray();
            array.Add(value.x);
            array.Add(value.y);
            return array;
        }
        if (obj is Color)
        {
            var value = (Color)obj;
            JArray array = new JArray();
            array.Add(value.r);
            array.Add(value.g);
            array.Add(value.b);
            array.Add(value.a);
            return array;
        }
        if (obj is Color32)
        {
            var value = (Color32)obj;
            JArray array = new JArray();
            array.Add(value.r / (float)255);
            array.Add(value.g / (float)255);
            array.Add(value.b / (float)255);
            array.Add(value.a / (float)255);
            return array;
        }

        if (changeLink)
        {
            if (BakeUnity.hashToGuidTable.TryGetValue(obj.GetHashCode(), out string guid))
                return guid;
            return "null";
        }
        else
        {
            if (obj is Material)
            {
                var material = obj as Material;

                Dictionary<string, List<string>> propertyNameTable = new Dictionary<string, List<string>>();
                propertyNameTable.Add("texture",
                    material.GetPropertyNames(MaterialPropertyType.Texture).ToList());
                propertyNameTable.Add("float",
                    material.GetPropertyNames(MaterialPropertyType.Float).ToList());
                propertyNameTable.Add("vector",
                    material.GetPropertyNames(MaterialPropertyType.Vector).ToList());
                propertyNameTable.Add("matrix",
                    material.GetPropertyNames(MaterialPropertyType.Matrix).ToList());
                propertyNameTable.Add("int",
                    material.GetPropertyNames(MaterialPropertyType.Int).ToList());

                JObject materialJson = new JObject();
                JObject dataJson = new JObject();

                materialJson.Add("name", material.name);
                if (BakeUnity.TryGetGuid(material, out var guid))
                    materialJson.Add("guid", guid);
                materialJson.Add("shaderName", material.shader.name.Split("/")[^1]);
                materialJson.Add("renderOrder", material.renderQueue);
                switch (material.GetInt("_Cull"))
                {
                    case 0:
                        materialJson.Add("culling", "both");
                        break;
                    case 1:
                        materialJson.Add("culling", "front");
                        break;
                    case 2:
                        materialJson.Add("culling", "back");
                        break;
                }

                materialJson.Add("datas", dataJson);


                JArray textureDatas = new JArray();
                dataJson.Add("textures", textureDatas);
                foreach (var value in propertyNameTable["texture"])
                {
                    JObject data = new JObject();
                    data["name"] = value;

                    var path = AssetDatabase.GetAssetPath(material.GetTexture(value)).Trim();
                    path = path.Replace("Assets/", BakeUnity.definePath_Resources);
                    var fileName = path.Split("/").Last();
                    var name = string.Join(".", fileName.Split(".")[0..^1]);

                    data["path"] = path;
                    data["fileName"] = fileName;
                    data["originalName"] = name;
                    if (!string.IsNullOrEmpty(path))
                        textureDatas.Add(data);
                }


                JArray floatDatas = new JArray();
                dataJson.Add("floats", floatDatas);
                foreach (var value in propertyNameTable["float"])
                {
                    JObject data = new JObject();
                    data["name"] = value;
                    data["data"] = material.GetFloat(value);
                    floatDatas.Add(data);
                }


                JArray intDatas = new JArray();
                dataJson.Add("ints", intDatas);
                foreach (var value in propertyNameTable["int"])
                {
                    JObject data = new JObject();
                    data["name"] = value;
                    data["data"] = material.GetInt(value);
                    intDatas.Add(data);
                }

                JArray vectorDatas = new JArray();
                dataJson.Add("vectors", vectorDatas);
                foreach (var value in propertyNameTable["vector"])
                {
                    JObject data = new JObject();
                    data["name"] = value;
                    data["data"] = ToJson(material.GetVector(value));
                    vectorDatas.Add(data);
                }

                JArray matrixDatas = new JArray();
                dataJson.Add("matrixs", matrixDatas);
                foreach (var value in propertyNameTable["matrix"])
                {
                    JObject data = new JObject();
                    data["name"] = value;
                    data["data"] = ToJson(material.GetMatrix(value));
                    matrixDatas.Add(data);
                }

                return materialJson;
            }


            if (obj is Mesh)
            {
                var mesh = obj as Mesh;
                JObject data = new JObject();
                var path = AssetDatabase.GetAssetPath(mesh).Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    switch (path)
                    {
                        case "Library/unity default resources":
                            {
                                if (!BakeUnity.nameToPathTable.ContainsKey(mesh.name))
                                    BakeUnity.nameToPathTable[mesh.name] = mesh.name + ".fbx";

                                path = BakeUnity.definePath_Resources + "Models/" + BakeUnity.nameToPathTable[mesh.name];
                                break;
                            }
                        default:
                            {
                                path = path.Replace("Assets/", BakeUnity.definePath_Resources);
                                break;
                            }
                    }
                    var fileName = path.Split("/").Last();
                    var name = string.Join(".", fileName.Split(".")[0..^1]);

                    data["path"] = path;
                    data["fileName"] = fileName;
                    data["modelName"] = name;
                    data["meshName"] = mesh.name;
                    data["boundCenter"] = ToJson(mesh.bounds.center);
                    data["boundExtent"] = ToJson(mesh.bounds.extents);
                }
                return data;
            }

        }
        //string jsonString = System.Text.Encoding.UTF8.GetString(SerializationUtility.SerializeValue(obj, DataFormat.JSON));
        return new JObject(obj);
    }
}
