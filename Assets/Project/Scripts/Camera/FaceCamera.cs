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

        Vector3 direction = transform.position - targetCamera.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}