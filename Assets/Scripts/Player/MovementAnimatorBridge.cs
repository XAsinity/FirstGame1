using UnityEngine;

/// <summary>
/// MovementAnimatorBridge: moves the CharacterController (optional) and updates Animator parameters.
/// - Safe: checks for parameter existence before setting them.
/// - Allows runtime assignment of the Animator via AssignAnimator(Animator).
/// - Will not spam the console if Animator is not present at Awake (since PlayerManager assigns it later).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MovementAnimatorBridge : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("If true this component moves the CharacterController itself. If false, it only updates Animator params.")]
    public bool driveMovement = false;

    [Header("Movement (used only when driveMovement=true)")]
    public float moveSpeed = 4f;
    public float gravity = -9.81f;
    public float rotationSmoothTime = 0.08f;

    [Header("Animator Parameters")]
    public string speedParam = "Speed";
    public string isMovingParam = "IsMoving";
    public string primaryAttackTrigger = "PrimaryAttack";

    [Tooltip("If true, Speed will be normalized to 0..1 using maxSpeedForNormalization")]
    public bool normalizeSpeed = false;
    public float maxSpeedForNormalization = 6f;

    [Tooltip("Optional: reference to PlayerController (bridge will try to auto-find)")]
    public PlayerController playerController;

    [Header("Debug")]
    [Tooltip("If true, log when animator is missing at Awake (default false since PlayerManager may assign later)")]
    public bool logMissingAnimatorAtAwake = false;

    // runtime
    private CharacterController cc;
    private Animator animator;
    private Vector3 velocity;
    private float turnSmoothVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // try to auto-find animator in children (may return null if visual is spawned later)
        animator = GetComponentInChildren<Animator>(true);
        if (animator == null && logMissingAnimatorAtAwake)
            Debug.LogWarning("MovementAnimatorBridge: no Animator found in children at Awake. PlayerManager may assign it later.");
    }

    void Update()
    {
        if (driveMovement)
        {
            HandleMovement();
        }

        UpdateAnimator();
    }

    void HandleMovement()
    {
        if (cc == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        Transform camT = Camera.main ? Camera.main.transform : null;
        Vector3 move = Vector3.zero;
        if (dir.magnitude >= 0.01f)
        {
            if (camT != null)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;
                move = (camForward * dir.z + camRight * dir.x).normalized;
            }
            else move = transform.TransformDirection(dir);

            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        if (!cc.isGrounded) velocity.y += gravity * Time.deltaTime;
        else if (velocity.y < 0f) velocity.y = -1f;

        Vector3 final = move * moveSpeed;
        final.y = velocity.y;
        cc.Move(final * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        // If animator is null, nothing to set (PlayerManager should assign it at spawn)
        if (animator == null)
            return;

        float speed = 0f;
        if (driveMovement)
        {
            Vector3 hv = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
            speed = hv.magnitude;
        }
        else
        {
            // Prefer PlayerController.CurrentSpeed if available
            if (playerController != null)
                speed = playerController.CurrentSpeed;
            else
            {
                Vector3 hv = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
                speed = hv.magnitude;
            }
        }

        float outSpeed = speed;
        if (normalizeSpeed)
        {
            float denom = Mathf.Approximately(maxSpeedForNormalization, 0f) ? 1f : maxSpeedForNormalization;
            outSpeed = Mathf.Clamp01(speed / denom);
        }

        if (!string.IsNullOrEmpty(speedParam) && HasAnimatorParameter(speedParam, AnimatorControllerParameterType.Float))
            animator.SetFloat(speedParam, outSpeed);

        if (!string.IsNullOrEmpty(isMovingParam) && HasAnimatorParameter(isMovingParam, AnimatorControllerParameterType.Bool))
            animator.SetBool(isMovingParam, speed > 0.05f);
    }

    /// <summary>
    /// Assign the animator instance at runtime (PlayerManager uses reflection but calling directly is cleaner).
    /// </summary>
    public void AssignAnimator(Animator a)
    {
        animator = a;
        // optional: log for verification
        Debug.Log($"MovementAnimatorBridge: Animator assigned at runtime -> {(animator != null ? animator.gameObject.name : "null")}");
    }

    /// <summary>
    /// Safely trigger an attack trigger if it exists.
    /// </summary>
    public void TriggerAttack(string triggerName = null)
    {
        if (animator == null) return;
        string t = string.IsNullOrEmpty(triggerName) ? primaryAttackTrigger : triggerName;
        if (HasAnimatorParameter(t, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(t);
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in animator.parameters)
            if (p.name == paramName && p.type == type) return true;
        return false;
    }
}