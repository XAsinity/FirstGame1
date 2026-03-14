using UnityEngine;

/// <summary>
/// PlayerController: simple movement + animator hookup helper.
/// - Moves the CharacterController (basic WASD movement).
/// - Auto-finds an Animator in children on Awake if one isn't assigned.
/// - Exposes AssignAnimator(Animator) so PlayerManager can assign the visual's Animator at spawn time.
/// - Exposes CurrentSpeed and Anim so other systems (MovementAnimatorBridge, PlayerManager) can read them.
/// - Safely checks for Animator parameters before setting triggers to avoid "Parameter does not exist" errors.
/// Replace or merge with your existing PlayerController implementation as needed.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in world units per second")]
    public float MoveSpeed = 25f;

    [Tooltip("Rotation smoothing time for facing the movement direction")]
    public float rotationSmoothTime = 0.08f;

    [Header("Cursor Rotation")]
    [Tooltip("When true the character always faces the mouse cursor (Diablo style). When false it faces the movement direction.")]
    public bool lookAtCursor = true;

    [Tooltip("Layer mask used for the cursor ground plane raycast. Set to the layer(s) your ground geometry uses.")]
    public LayerMask cursorLayerMask = ~0;

    [Header("Animation (optional)")]
    [Tooltip("Assign the Animator from the model child, or leave empty to auto-find at Awake.")]
    [SerializeField] private Animator animator;

    // Exposed to other scripts
    public Animator Anim => animator;
    public float CurrentSpeed { get; private set; }

    // Internal
    private CharacterController cc;
    private Vector3 velocity;
    private float turnSmoothVel;
    private Camera mainCamera;

    // Minimum squared look-direction magnitude to avoid snapping to zero
    private const float LookDirSqrThreshold = 0.001f;
    // Maximum raycast distance for cursor-to-world projection
    private const float CursorRaycastDistance = 300f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        // Try to auto-find an Animator in children (works for prefab instances that already contain the model).
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
                Debug.Log($"PlayerController: Auto-found Animator on '{animator.gameObject.name}'.");
            else
                Debug.Log("PlayerController: No Animator assigned or found in children at Awake. PlayerManager can AssignAnimator at spawn.");
        }
    }

    void Update()
    {
        HandleMovement();
        UpdateAnimatorParams();
    }

    private void HandleMovement()
    {
        // Basic input-driven movement (camera relative).
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        Vector3 move = Vector3.zero;
        Transform camT = mainCamera ? mainCamera.transform : null;

        if (dir.magnitude >= 0.01f)
        {
            if (camT != null)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;
                move = (camForward * dir.z + camRight * dir.x).normalized;
            }
            else
            {
                move = transform.TransformDirection(dir);
            }

            if (!lookAtCursor)
            {
                // Rotate toward movement direction when cursor aiming is off
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        if (lookAtCursor)
        {
            // Rotate to face the mouse cursor using a horizontal plane at the player's feet
            Camera cam = mainCamera;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                // Use Physics.Raycast first (hits actual ground geometry)
                if (Physics.Raycast(ray, out RaycastHit hit, CursorRaycastDistance, cursorLayerMask))
                {
                    Vector3 lookTarget = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                    Vector3 lookDir = lookTarget - transform.position;
                    if (lookDir.sqrMagnitude > LookDirSqrThreshold)
                        transform.rotation = Quaternion.LookRotation(lookDir);
                }
                else
                {
                    // Fallback: intersect ray with a flat plane at the player's height
                    Plane groundPlane = new Plane(Vector3.up, transform.position);
                    if (groundPlane.Raycast(ray, out float enter))
                    {
                        Vector3 worldPoint = ray.GetPoint(enter);
                        Vector3 lookDir = new Vector3(worldPoint.x - transform.position.x, 0f, worldPoint.z - transform.position.z);
                        if (lookDir.sqrMagnitude > LookDirSqrThreshold)
                            transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }
        }

        // Gravity
        if (!cc.isGrounded)
            velocity.y += Physics.gravity.y * Time.deltaTime;
        else if (velocity.y < 0f)
            velocity.y = -1f;

        Vector3 final = move * MoveSpeed;
        final.y = velocity.y;
        cc.Move(final * Time.deltaTime);
    }

    private void UpdateAnimatorParams()
    {
        if (animator == null)
            return;

        Vector3 horizVel = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        float speed = horizVel.magnitude;
        CurrentSpeed = speed;

        // Set 'Speed' float if it exists
        if (HasAnimatorParameter("Speed", AnimatorControllerParameterType.Float))
            animator.SetFloat("Speed", speed);

        // Set 'IsMoving' bool if it exists
        if (HasAnimatorParameter("IsMoving", AnimatorControllerParameterType.Bool))
            animator.SetBool("IsMoving", speed > 0.05f);
    }

    /// <summary>
    /// Assigns the runtime Animator instance (usually from the spawned visual prefab).
    /// Call this from PlayerManager right after instantiating the visual so the capsule has a valid Animator reference.
    /// </summary>
    /// <param name="a">Animator instance found on the spawned visual</param>
    public void AssignAnimator(Animator a)
    {
        if (a == null)
        {
            Debug.LogWarning("PlayerController.AssignAnimator called with null.");
            return;
        }

        animator = a;
        Debug.Log($"PlayerController: Animator assigned at runtime -> {animator.gameObject.name}");
    }

    /// <summary>
    /// Safely trigger the PrimaryAttack trigger on the animator (only if the parameter exists).
    /// </summary>
    public void TriggerPrimaryAttack()
    {
        if (animator == null)
        {
            Debug.LogWarning("PlayerController.TriggerPrimaryAttack called but animator is null.");
            return;
        }

        if (HasAnimatorParameter("PrimaryAttack", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("PrimaryAttack");
        else
            Debug.LogWarning("PlayerController: Animator does not have trigger 'PrimaryAttack'. Add it to the PlayerAnimator or change the trigger name.");
    }

    /// <summary>
    /// Helper to check if an animator parameter exists and matches the requested type.
    /// </summary>
    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == type) return true;
        }
        return false;
    }
}