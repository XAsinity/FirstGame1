using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;

    [Header("Animation")]
    [Tooltip("Drag the child 3D model that has the Animator component into this slot")]
    public Animator anim;

    private CharacterController controller;
    private Camera mainCamera;

    void Start()
    {
        // Automatically grab the components we need
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        MovePlayer();
        LookAtMouse();
    }

    void MovePlayer()
    {
        // 1. Get input from WASD or Arrow Keys
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 2. Calculate movement direction (normalized so diagonal movement isn't faster)
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // 3. Move the character using the Character Controller
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 4. Send the speed to the Animator so it knows when to transition to the Walk animation
        if (anim != null)
        {
            // moveDirection.magnitude will be 0 when standing still, and 1 when moving
            anim.SetFloat("Speed", moveDirection.magnitude);
        }
    }

    void LookAtMouse()
    {
        // 1. Create a ray from the mouse position into the 3D world
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 2. Create a mathematical plane at the player's exact height (so aiming works on ramps!)
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float rayDistance;

        // 3. If the ray hits the plane, find that exact point
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 pointToLook = ray.GetPoint(rayDistance);

            // 4. Look at the point (keeping the player perfectly upright)
            Vector3 lookTarget = new Vector3(pointToLook.x, transform.position.y, pointToLook.z);
            transform.LookAt(lookTarget);
        }
    }
}