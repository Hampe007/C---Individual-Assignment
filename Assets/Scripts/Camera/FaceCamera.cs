using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void OnEnable()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        transform.rotation = targetCamera.transform.rotation;
    }
}
