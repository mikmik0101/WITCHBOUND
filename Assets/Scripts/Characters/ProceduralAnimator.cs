using UnityEngine;

public enum LimbRig
{
    Humanoid,
    Quadruped,
    Winged,
    Crawler
}

/// <summary>
/// Cheap procedural locomotion. The owner sets <see cref="speed"/> (world units
/// per second) each frame and the rig swings actual limb transforms - upper legs,
/// lower legs, arms, wings, quadruped legs - around their joints. No root
/// translation is used for the animation itself.
/// </summary>
public class ProceduralAnimator : MonoBehaviour
{
    public LimbRig rig = LimbRig.Humanoid;
    public float speed = 0f;         // set by the owner (PlayerController / AI)
    public float stride = 2.4f;      // metres travelled per full limb cycle
    public float swingScale = 1f;

    [Header("Humanoid rig")]
    public Transform leftUpperLeg, rightUpperLeg;
    public Transform leftLowerLeg, rightLowerLeg;
    public Transform leftArm, rightArm;
    public Transform torso;
    public Transform head;

    [Header("Quadruped rig")]
    public Transform[] frontLegs;
    public Transform[] backLegs;

    [Header("Winged rig")]
    public Transform leftWing, rightWing;

    private float _phase;
    private float _amp;
    private float _breath;

    private float _attackTimer;
    private float _attackDuration;
    private float _attackWindup;
    private bool _attacking;

    /// <summary>True while the attack overlay is driving the limbs.</summary>
    public bool IsAttacking { get { return _attacking; } }

    private struct Rest { public Transform t; public Vector3 pos; public Quaternion rot; }
    private Rest[] _rest;

    private bool _captured;

    /// <summary>
    /// Rest pose is captured lazily on the first Update, because the rig fields
    /// are assigned by the factory AFTER the component is added - capturing in
    /// Awake would leave the rest list empty and make limbs spin.
    /// </summary>
    private void CaptureRest()
    {
        var list = new System.Collections.Generic.List<Rest>();
        Add(list, leftUpperLeg); Add(list, rightUpperLeg);
        Add(list, leftLowerLeg); Add(list, rightLowerLeg);
        Add(list, leftArm); Add(list, rightArm);
        Add(list, torso); Add(list, head);
        Add(list, leftWing); Add(list, rightWing);
        if (frontLegs != null) foreach (var f in frontLegs) Add(list, f);
        if (backLegs != null) foreach (var b in backLegs) Add(list, b);
        _rest = list.ToArray();
        _captured = true;
    }

    private static void Add(System.Collections.Generic.List<Rest> list, Transform t)
    {
        if (t == null) return;
        list.Add(new Rest { t = t, pos = t.localPosition, rot = t.localRotation });
    }

    private void Update()
    {
        if (!_captured) CaptureRest();
        if (_rest == null || _rest.Length == 0) return;

        bool moving = speed > 0.15f;
        float cyclesPerSecond = moving ? Mathf.Max(0.5f, speed / Mathf.Max(0.4f, stride)) : 0f;
        _phase += cyclesPerSecond * Mathf.PI * 2f * Time.deltaTime;
        if (_phase > 1e6f) _phase = 0f;

        // Amplitude ramps with speed: idle is almost still, walking is clear and
        // a 6.5 m/s chase reads as a full sprint rather than a slow wade.
        float target = moving ? Mathf.Clamp(speed / 5f, 0.40f, 1.25f) : 0f;
        _amp = Mathf.Lerp(_amp, target, 8f * Time.deltaTime);
        _breath += Time.deltaTime * 1.6f;

        float s = Mathf.Sin(_phase);
        float sOpp = Mathf.Sin(_phase + Mathf.PI);
        float swing = 46f * _amp * swingScale;      // degrees
        float bend = 52f * _amp * swingScale;
        float idle = (1f - Mathf.Clamp01(_amp)) * 1.6f; // subtle breathing when idle

        for (int i = 0; i < _rest.Length; i++)
            _rest[i].t.localRotation = _rest[i].rot;
        for (int i = 0; i < _rest.Length; i++)
            _rest[i].t.localPosition = _rest[i].pos;

        switch (rig)
        {
            case LimbRig.Humanoid:
                Swing(leftUpperLeg, s * swing);
                Swing(rightUpperLeg, sOpp * swing);
                Swing(leftLowerLeg, Mathf.Max(0f, -s) * bend);
                Swing(rightLowerLeg, Mathf.Max(0f, -sOpp) * bend);
                Swing(leftArm, sOpp * swing * 0.75f);
                Swing(rightArm, s * swing * 0.75f);
                if (torso != null)
                {
                    var p = torso.localPosition;
                    // Two bobs per stride (one per footfall) plus a little lean.
                    p.y += Mathf.Abs(Mathf.Sin(_phase)) * 0.075f * _amp + Mathf.Sin(_breath) * 0.006f * idle;
                    p.z += Mathf.Sin(_phase * 2f) * 0.012f * _amp;
                    torso.localPosition = p;
                }
                if (head != null)
                    Swing(head, -Mathf.Abs(s) * 3.5f * _amp);
                break;

            case LimbRig.Crawler:
                Swing(leftArm, s * swing * 1.3f);
                Swing(rightArm, sOpp * swing * 1.3f);
                Swing(leftUpperLeg, sOpp * swing);
                Swing(rightUpperLeg, s * swing);
                if (torso != null)
                {
                    var p = torso.localPosition;
                    p.y += Mathf.Abs(Mathf.Sin(_phase * 2f)) * 0.04f * _amp;
                    torso.localPosition = p;
                }
                break;

            case LimbRig.Quadruped:
                AnimateArray(frontLegs, s, swing * 0.7f, 0f);
                AnimateArray(backLegs, sOpp, swing * 0.7f, 0f);
                if (torso != null)
                {
                    var p = torso.localPosition;
                    p.y += Mathf.Abs(Mathf.Sin(_phase)) * 0.05f * _amp;
                    torso.localPosition = p;
                }
                if (head != null) Swing(head, Mathf.Sin(_phase * 2f) * 3f);
                break;

            case LimbRig.Winged:
                // Faster flap while sprinting, but capped so the wings never
                // spin into a blur at 6.5 m/s.
                float flap = Mathf.Sin(Time.time * (3.5f + Mathf.Min(_amp, 1.1f) * 4.5f)) * (10f + 22f * Mathf.Min(_amp, 1.1f));
                if (leftWing != null) leftWing.localRotation = leftWing.localRotation * Quaternion.Euler(0f, 0f, -Mathf.Abs(flap));
                if (rightWing != null) rightWing.localRotation = rightWing.localRotation * Quaternion.Euler(0f, 0f, Mathf.Abs(flap));
                if (torso != null)
                {
                    var p = torso.localPosition;
                    p.y += Mathf.Sin(Time.time * 1.6f) * 0.12f + Mathf.Sin(_breath) * 0.01f;
                    torso.localPosition = p;
                }
                Swing(leftArm, s * swing * 0.5f);
                Swing(rightArm, sOpp * swing * 0.5f);
                break;
        }

        // Attack overlay is applied after locomotion so the strike always reads
        // clearly: wind the body back, snap forward on impact, settle on recovery.
        if (_attacking) ApplyAttackPose();

        // if not moving, gently return limbs (already reset above) - nothing else to do
    }

    /// <summary>
    /// Starts a procedural attack pose. The owner calls this when the wind-up
    /// begins; the overlay runs for windup + recovery seconds and then clears
    /// itself, so no animation controller or clip is required.
    /// </summary>
    public void PlayAttack(float windup, float recovery)
    {
        _attackWindup = Mathf.Max(0.05f, windup);
        _attackDuration = _attackWindup + Mathf.Max(0.05f, recovery);
        _attackTimer = 0f;
        _attacking = true;
    }

    public void CancelAttack()
    {
        _attacking = false;
        _attackTimer = 0f;
    }

    private void ApplyAttackPose()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackDuration)
        {
            _attacking = false;
            return;
        }

        float pose;
        if (_attackTimer <= _attackWindup)
        {
            // -1 = fully wound back / limbs raised for the strike.
            pose = -Mathf.SmoothStep(0f, 1f, _attackTimer / _attackWindup);
        }
        else
        {
            // +1 at the impact frame, easing back to neutral through recovery.
            float r = (_attackTimer - _attackWindup) / Mathf.Max(0.01f, _attackDuration - _attackWindup);
            pose = Mathf.Lerp(1f, 0f, r);
        }

        switch (rig)
        {
            case LimbRig.Humanoid:
                Swing(leftArm, -78f * pose);
                Swing(rightArm, -78f * pose);
                if (torso != null) Swing(torso, 16f * pose);
                if (head != null) Swing(head, -10f * pose);
                break;

            case LimbRig.Crawler:
                Swing(leftArm, -92f * pose);
                Swing(rightArm, -92f * pose);
                if (torso != null) Swing(torso, 14f * pose);
                break;

            case LimbRig.Quadruped:
                if (head != null) Swing(head, -26f * pose);
                if (torso != null) Swing(torso, 10f * pose);
                AnimateArray(frontLegs, -0.9f * pose, 24f, 0f);
                break;

            case LimbRig.Winged:
                float flare = 42f * pose;
                if (leftWing != null) leftWing.localRotation = leftWing.localRotation * Quaternion.Euler(0f, 0f, -flare);
                if (rightWing != null) rightWing.localRotation = rightWing.localRotation * Quaternion.Euler(0f, 0f, flare);
                Swing(leftArm, -70f * pose);
                Swing(rightArm, -70f * pose);
                if (torso != null) Swing(torso, 14f * pose);
                break;
        }
    }

    private void AnimateArray(Transform[] arr, float phaseSin, float swing, float bend)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) Swing(arr[i], phaseSin * swing);
    }

    // All limbs are reset to their rest rotation at the start of Update, so a
    // single multiply applies the swing from rest without any lookup.
    private void Swing(Transform t, float degrees)
    {
        if (t == null) return;
        t.localRotation = t.localRotation * Quaternion.Euler(degrees, 0f, 0f);
    }
}
