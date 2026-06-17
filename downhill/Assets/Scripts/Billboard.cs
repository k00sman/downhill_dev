using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        Debug.Log("Camera found: " + cam);
    }

    void LateUpdate()
    {
        if (cam == null) return;
        transform.forward = cam.transform.forward;
    }
}