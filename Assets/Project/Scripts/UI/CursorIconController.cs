using UnityEngine;
using UnityEngine.InputSystem;

public class CursorIconController : MonoBehaviour
{
    public Texture2D normalCursor;
    public Texture2D leftClickCursor;
    public Texture2D rightClickCursor;

    private static CursorIconController instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Cursor.SetCursor(normalCursor, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.isPressed)
            Cursor.SetCursor(leftClickCursor, Vector2.zero, CursorMode.Auto);
        else if (Mouse.current.rightButton.isPressed)
            Cursor.SetCursor(rightClickCursor, Vector2.zero, CursorMode.Auto);
        else
            Cursor.SetCursor(normalCursor, Vector2.zero, CursorMode.Auto);
    }
}