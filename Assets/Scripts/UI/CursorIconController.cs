using UnityEngine;
using UnityEngine.InputSystem;

public class CursorIconController : MonoBehaviour
{
    public Texture2D NormalCursor;
    public Texture2D LeftClickCursor;
    public Texture2D RightClickCursor;
    private static CursorIconController instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Cursor.SetCursor(NormalCursor, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Cursor.SetCursor(LeftClickCursor, Vector2.zero, CursorMode.Auto);
        }
        else if (Mouse.current.rightButton.isPressed)
        {
            Cursor.SetCursor(RightClickCursor, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(NormalCursor, Vector2.zero, CursorMode.Auto);
        }
    }
}