using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    
    [SerializeField] private float mouseSensitivity = 1f;
    public float HorizontalInput {private set; get;}
    public float VerticalInput {private set; get;}
    public bool JumpInput {private set; get;}
    public bool SprintInput {private set; get;}
    public bool WalkInput {private set; get;}
    public bool RightClickInput {private set; get;}
    public float MouseX {private set; get;}
    public float MouseY {private set; get;}

    
    
    private void Update()
    {
        GetInput();
    }
    
    
    private void GetInput()
    {
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        VerticalInput = Input.GetAxisRaw("Vertical");
        JumpInput = Input.GetButtonDown("Jump");
        SprintInput = Input.GetButton("Sprint");
        WalkInput = Input.GetButton("Walk");
        RightClickInput = Input.GetMouseButton(1);
        MouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        MouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
    }
}
