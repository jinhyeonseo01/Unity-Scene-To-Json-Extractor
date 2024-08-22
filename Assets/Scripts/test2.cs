using UnityEngine;
using UnityEngine.UI;

public class test2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform target;
    public Canvas canvas;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Camera.main.WorldToScreenPoint(target.position), 
            canvas.worldCamera,
            out Vector2 pos))
        {
            (transform as RectTransform).localPosition = pos;
        }

    }
}
