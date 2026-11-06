using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;
    public float smoothTime = 0.1f;
    // Hard ceiling on how far one frame may turn the view. X11/Unity can report
    // a multi-thousand-pixel delta on the first frame after the pointer is
    // re-locked (pause -> resume, alt-tab back in). Clamping keeps that frame
    // from whipping the camera around.
    public float maxTurnPerFrame = 30f;

    [Header("References")]
    public Transform playerBody;
    private PlayerController playerController;

    private Vector3 baseLocalPosition;
    private float verticalRotation = 0f;
    private Vector2 smoothMouseDelta;
    // Set when a transition (pause exit, focus regain, respawn) means the next
    // delta is not a real hand movement and must be dropped instead of applied.
    private bool resetLookInput;
    private bool wasBlockedLastFrame = true;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;
    private bool shakeOffsetApplied;

    private bool logLookInput;
    private int lookLogFrame;
    private float lookLogTimer;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
        if (playerBody == null && playerController != null)
            playerBody = playerController.transform;

        try
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-mouselookdiag") logLookInput = true;
        }
        catch { }
    }

    private void Start()
    {
        // Cursor state managed by InputStateManager
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // The pointer is warped while unfocused; the delta reported on the first
        // focused frame is not a hand movement.
        if (hasFocus) resetLookInput = true;
    }

    private void Update()
    {
        bool blocked = (InputStateManager.Instance != null && InputStateManager.Instance.IsMenuMode())
                       || (playerController != null && playerController.IsDead);

        if (blocked)
        {
            // Leaving gameplay drops any in-flight smoothing so resuming never
            // replays a stale delta.
            smoothMouseDelta = Vector2.zero;
            wasBlockedLastFrame = true;
            return;
        }

        if (wasBlockedLastFrame)
        {
            resetLookInput = true;
            wasBlockedLastFrame = false;
        }

        HandleMouseLook();
    }

    private void LateUpdate()
    {
        ApplyShake();
    }

    /// <summary>
    /// Short impact shake. Kept additive in LateUpdate so it never fights the
    /// mouse-look rotation written in Update.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (playerController != null && playerController.IsDead) return;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
        shakeMagnitude = magnitude;
    }

    private void ApplyShake()
    {
        if (playerController != null && playerController.IsDead) return;

        if (shakeTimer <= 0f)
        {
            if (shakeOffsetApplied)
            {
                transform.localPosition = baseLocalPosition;
                shakeOffsetApplied = false;
            }
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(shakeTimer / shakeDuration) * shakeMagnitude;
        transform.localPosition = baseLocalPosition + Random.insideUnitSphere * k;
        transform.localRotation = transform.localRotation *
            Quaternion.Euler(Random.Range(-1f, 1f) * k * 25f,
                             Random.Range(-1f, 1f) * k * 25f,
                             Random.Range(-1f, 1f) * k * 10f);
        shakeOffsetApplied = true;
    }

    /// <summary>Restores default yaw/pitch and camera offset for a new session.</summary>
    public void ResetLook()
    {
        verticalRotation = 0f;
        smoothMouseDelta = Vector2.zero;
        resetLookInput = true;
        shakeTimer = 0f;
        shakeOffsetApplied = false;

        transform.localPosition = baseLocalPosition;
        transform.localRotation = Quaternion.identity;
    }

    private void HandleMouseLook()
    {
        // Relative pointer motion, read straight from the axis every frame.
        // Nothing here depends on a mouse button, on the movement keys, on
        // Time.timeScale or on whether the player is moving: a touchpad and a
        // USB mouse both arrive as "Mouse X"/"Mouse Y" deltas and are treated
        // identically. GetAxisRaw is used so no axis-side smoothing can round a
        // small touchpad delta down to nothing.
        float rawX = Input.GetAxisRaw("Mouse X");
        float rawY = Input.GetAxisRaw("Mouse Y");

        if (resetLookInput)
        {
            // Drop this frame's delta entirely: after a cursor re-lock it is a
            // pointer-warp artefact, not a hand movement.
            resetLookInput = false;
            smoothMouseDelta = Vector2.zero;
            LogLook(rawX, rawY, 0f, 0f);
            return;
        }

        // Frame-rate independent, unity-gain low pass: at steady input the
        // filtered value equals the raw delta, so no motion is ever lost, but a
        // single noisy touchpad frame cannot snap the view.
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) dt = 1f / 60f;
        float alpha = smoothTime <= 0.0001f ? 1f : 1f - Mathf.Exp(-dt / smoothTime);

        smoothMouseDelta.x = Mathf.Lerp(smoothMouseDelta.x, rawX, alpha);
        smoothMouseDelta.y = Mathf.Lerp(smoothMouseDelta.y, rawY, alpha);

        float turnX = Mathf.Clamp(smoothMouseDelta.x * mouseSensitivity, -maxTurnPerFrame, maxTurnPerFrame);
        float turnY = Mathf.Clamp(smoothMouseDelta.y * mouseSensitivity, -maxTurnPerFrame, maxTurnPerFrame);

        verticalRotation -= turnY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        if (playerBody != null)
        {
            // Yaw lives on the body, so holding W keeps moving along the new
            // facing while the camera turns underneath it.
            playerBody.Rotate(Vector3.up * turnX);
        }

        LogLook(rawX, rawY, turnX, turnY);
    }

    /// <summary>
    /// QA-only (-mouselookdiag): prints the raw pointer delta and whether a
    /// movement key is held, so a physical touchpad can be checked against the
    /// exact numbers the game receives. Silent in normal play.
    /// </summary>
    private void LogLook(float rawX, float rawY, float turnX, float turnY)
    {
        if (!logLookInput) return;

        lookLogTimer += Time.unscaledDeltaTime;
        bool moving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
                      || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        bool nonZero = Mathf.Abs(rawX) > 0.0001f || Mathf.Abs(rawY) > 0.0001f;

        // Log every 0.25 s, plus every frame that actually carries motion so a
        // touchpad sweep during W cannot be missed between samples.
        if (nonZero || lookLogTimer >= 0.25f)
        {
            lookLogTimer = 0f;
            lookLogFrame++;
            Debug.Log($"[LOOK] f={lookLogFrame} raw=({rawX:F4},{rawY:F4}) applied=({turnX:F3},{turnY:F3}) " +
                      $"keys={(moving ? "WASD" : "none")} lock={Cursor.lockState} vis={Cursor.visible} " +
                      $"mode={(InputStateManager.Instance != null ? InputStateManager.Instance.currentMode.ToString() : "?")} " +
                      $"dt={Time.unscaledDeltaTime:F4}");
        }
    }

    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
}
