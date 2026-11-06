using UnityEngine;

/// <summary>
/// Builds low-poly characters at runtime with proper joint pivots so limbs can
/// be animated, plus a replaceable face quad.
///
/// Survivors: full humanoid (head, hair, face, torso, arms, legs, feet).
/// Witches:   six distinct silhouettes (Humanoid / Winged / Hound / Crawler /
///            Tall / Final).
/// Player:    headless body for first-person (you see your own legs).
///
/// Part colliders are stripped; a single collider is placed on the root.
/// NOTE: no MonoBehaviour classes live in this file (that would break the
/// built scene) - only the static factory.
/// </summary>
public static class RuntimeCharacterFactory
{
    // ============================================================ survivors

    public struct Look
    {
        public Color clothing, skin, hair;
        public bool hairLong;
        public Look(Color clothing, Color skin, Color hair, bool hairLong = false)
        { this.clothing = clothing; this.skin = skin; this.hair = hair; this.hairLong = hairLong; }
    }

    public static string[] SurvivorNames { get { return CharacterLore.SurvivorNames; } }

    public static Look GetSurvivorLook(string name)
    {
        switch (CharacterLore.OfficialName(name))
        {
            case "Luis Padilla": return new Look(new Color(0.18f, 0.30f, 0.52f), new Color(0.86f, 0.70f, 0.58f), new Color(0.26f, 0.17f, 0.09f));
            case "Mick Richards": return new Look(new Color(0.24f, 0.36f, 0.18f), new Color(0.62f, 0.45f, 0.33f), new Color(0.08f, 0.07f, 0.07f));
            case "Josh Garcia":  return new Look(new Color(0.55f, 0.28f, 0.16f), new Color(0.92f, 0.78f, 0.66f), new Color(0.72f, 0.50f, 0.24f));
            case "Vincent Reid": return new Look(new Color(0.36f, 0.20f, 0.42f), new Color(0.74f, 0.56f, 0.44f), new Color(0.12f, 0.09f, 0.06f));
            default:             return new Look(new Color(0.30f, 0.32f, 0.38f), new Color(0.85f, 0.72f, 0.60f), new Color(0.15f, 0.10f, 0.06f));
        }
    }

    public static GameObject BuildSurvivor(string name, Vector3 position, float yaw, Look? overrideLook = null)
    {
        Look look = overrideLook ?? GetSurvivorLook(name);
        var root = new GameObject("SurvivorModel_" + name.Replace(" ", "_"));
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Rig rig;
        Transform head, torso;
        BuildHumanoid(root.transform, look.clothing, look.skin, look.hair, look.hairLong, out rig, out head, out torso);

        var faceR = AttachFace(head, name, null, WitchType.Humanoid, look.skin);

        var visual = root.AddComponent<CharacterVisual>();
        visual.displayName = name; visual.isChaser = false;
        visual.headTransform = head;
        visual.faceRenderer = faceR; visual.faceMaterial = faceR.sharedMaterial;

        var anim = root.AddComponent<ProceduralAnimator>();
        ApplyHumanoidRig(anim, rig, torso, head);

        AddCapsuleCollider(root, 1.75f, 0.35f);
        return root;
    }

    /// <summary>Headless player body for first-person (look down to see legs).</summary>
    public static GameObject BuildPlayerBody(Color clothing, Color skin)
    {
        var root = new GameObject("PlayerBody");
        Rig rig; Transform head, torso;
        BuildHumanoid(root.transform, clothing, skin, skin, false, out rig, out head, out torso);
        // Head, torso and hips are hidden from the FPS view (the camera sits in
        // the head) - legs + arms remain so you can look down and see them move.
        if (head != null) Object.DestroyImmediate(head.gameObject);
        if (torso != null)
        {
            foreach (var t in torso.GetComponentsInChildren<Transform>())
                if (t != torso && (t.name == "Torso" || t.name == "Hips"))
                    Object.DestroyImmediate(t.gameObject);
        }

        var anim = root.AddComponent<ProceduralAnimator>();
        anim.rig = LimbRig.Humanoid;
        anim.leftUpperLeg = rig.lUL; anim.rightUpperLeg = rig.rUL;
        anim.leftLowerLeg = rig.lLL; anim.rightLowerLeg = rig.rLL;
        anim.leftArm = rig.lArm; anim.rightArm = rig.rArm;
        anim.torso = torso;
        return root;
    }

    // ============================================================== witches

    public static GameObject BuildWitch(string name, WitchType type, Vector3 position, float yaw)
    {
        var root = new GameObject("ChaserModel_" + name.Replace(" ", "_"));
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Transform head, torso;
        var anim = root.AddComponent<ProceduralAnimator>();
        Color glow = new Color(0.75f, 0.95f, 1f);
        Color accent = new Color(0.30f, 0.06f, 0.18f);
        float colH = 1.9f, colR = 0.4f;

        switch (type)
        {
            case WitchType.Winged:
                BuildWinged(root.transform, out head, out torso, anim, ref glow, ref accent);
                colH = 2.3f; colR = 0.5f;
                break;
            case WitchType.Hound:
                BuildHound(root.transform, out head, out torso, anim, ref glow, ref accent);
                colH = 1.2f; colR = 0.6f;
                break;
            case WitchType.Crawler:
                BuildCrawler(root.transform, out head, out torso, anim, ref glow, ref accent);
                colH = 1.1f; colR = 0.55f;
                break;
            case WitchType.Tall:
                BuildTall(root.transform, out head, out torso, anim, ref glow, ref accent);
                colH = 3.4f; colR = 0.5f;
                break;
            case WitchType.Final:
                BuildFinal(root.transform, out head, out torso, anim, ref glow, ref accent);
                colH = 2.0f; colR = 0.45f;
                break;
            default:
                BuildHumanoidWitch(root.transform, out head, out torso, anim, ref glow, ref accent);
                break;
        }

        // The head carries nothing but the character picture: no eyes, no VFX,
        // no decorations are ever attached to it.
        var faceR = AttachFace(head, null, name, type, new Color(0.80f, 0.78f, 0.76f));

        // supernatural chest sigil (body detail, not part of the head)
        var sigilMat = Emissive(glow, 2.0f, true);
        float sigilY = (type == WitchType.Hound || type == WitchType.Crawler) ? 0.75f : 1.25f;
        var sigil = Prim(PrimitiveType.Sphere, root.transform, "ChestSigil", new Vector3(0f, sigilY, 0.18f), Vector3.one * 0.16f, null);
        sigil.GetComponent<Renderer>().sharedMaterial = sigilMat;

        // cheap aura light
        var aura = new GameObject("AuraLight");
        aura.transform.SetParent(root.transform, false);
        aura.transform.localPosition = new Vector3(0f, colH * 0.55f, 0f);
        var l = aura.AddComponent<Light>();
        l.type = LightType.Point; l.color = accent; l.range = 7f; l.intensity = 1.0f;
        l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForceVertex;

        var visual = root.AddComponent<CharacterVisual>();
        visual.displayName = name; visual.isChaser = true;
        visual.headTransform = head; visual.faceRenderer = faceR; visual.faceMaterial = faceR.sharedMaterial;

        AddCapsuleCollider(root, colH, colR);
        return root;
    }

    private static void ApplyHumanoidRig(ProceduralAnimator anim, Rig rig, Transform torso, Transform head)
    {
        anim.rig = LimbRig.Humanoid;
        anim.leftUpperLeg = rig.lUL; anim.rightUpperLeg = rig.rUL;
        anim.leftLowerLeg = rig.lLL; anim.rightLowerLeg = rig.rLL;
        anim.leftArm = rig.lArm; anim.rightArm = rig.rArm;
        anim.torso = torso; anim.head = head;
    }

    private static void BuildHumanoidWitch(Transform root, out Transform head, out Transform torso, ProceduralAnimator anim, ref Color glow, ref Color accent)
    {
        Color body = new Color(0.06f, 0.045f, 0.08f);
        accent = new Color(0.30f, 0.06f, 0.18f);
        glow = new Color(0.75f, 0.95f, 1f);
        Rig rig;
        BuildHumanoid(root, body, new Color(0.80f, 0.79f, 0.77f), new Color(0.06f, 0.05f, 0.07f), true, out rig, out head, out torso);
        Prim(PrimitiveType.Cube, root, "Robe", new Vector3(0f, 0.62f, -0.05f), new Vector3(0.62f, 0.95f, 0.24f), accent);
        Prim(PrimitiveType.Cube, root, "Claw_L", new Vector3(-0.34f, 0.72f, 0.06f), new Vector3(0.15f, 0.20f, 0.20f), new Color(0.72f, 0.70f, 0.68f));
        Prim(PrimitiveType.Cube, root, "Claw_R", new Vector3(0.34f, 0.72f, 0.06f), new Vector3(0.15f, 0.20f, 0.20f), new Color(0.72f, 0.70f, 0.68f));
        ApplyHumanoidRig(anim, rig, torso, head);
    }

    private static void BuildTall(Transform root, out Transform head, out Transform torso, ProceduralAnimator anim, ref Color glow, ref Color accent)
    {
        Color body = new Color(0.08f, 0.05f, 0.11f);
        accent = new Color(0.45f, 0.10f, 0.12f);
        glow = new Color(1f, 0.95f, 0.7f);
        Rig rig;
        BuildHumanoid(root, body, new Color(0.92f, 0.86f, 0.80f), new Color(0.12f, 0.08f, 0.14f), true, out rig, out head, out torso);
        root.localScale = new Vector3(1.7f, 1.9f, 1.7f); // unusually tall
        ApplyHumanoidRig(anim, rig, torso, head);
        anim.swingScale = 0.7f;
    }

    private static void BuildFinal(Transform root, out Transform head, out Transform torso, ProceduralAnimator anim, ref Color glow, ref Color accent)
    {
        Color body = new Color(0.14f, 0.03f, 0.20f);
        accent = new Color(0.55f, 0.10f, 0.85f);
        glow = new Color(1f, 0.25f, 0.30f);
        Rig rig;
        BuildHumanoid(root, body, new Color(0.72f, 0.66f, 0.70f), new Color(0.20f, 0.05f, 0.28f), true, out rig, out head, out torso);
        Prim(PrimitiveType.Cube, root, "Robe", new Vector3(0f, 0.55f, -0.05f), new Vector3(0.72f, 1.05f, 0.28f), accent);
        // spiked shoulders for a distinctive final-boss silhouette (small + non-occluding)
        Prim(PrimitiveType.Cube, root, "Spike_L", new Vector3(-0.42f, 1.5f, 0f), new Vector3(0.14f, 0.5f, 0.14f), accent).transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        Prim(PrimitiveType.Cube, root, "Spike_R", new Vector3(0.42f, 1.5f, 0f), new Vector3(0.14f, 0.5f, 0.14f), accent).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        Prim(PrimitiveType.Cube, root, "Claw_L", new Vector3(-0.36f, 0.68f, 0.06f), new Vector3(0.16f, 0.26f, 0.24f), new Color(0.5f, 0.15f, 0.6f));
        Prim(PrimitiveType.Cube, root, "Claw_R", new Vector3(0.36f, 0.68f, 0.06f), new Vector3(0.16f, 0.26f, 0.24f), new Color(0.5f, 0.15f, 0.6f));
        ApplyHumanoidRig(anim, rig, torso, head);
    }

    private static void BuildWinged(Transform root, out Transform head, out Transform torso, ProceduralAnimator anim, ref Color glow, ref Color accent)
    {
        Color body = new Color(0.10f, 0.02f, 0.15f);
        accent = new Color(0.20f, 0.05f, 0.30f);
        glow = new Color(1f, 0.85f, 0.5f);

        torso = Pivot(root, "TorsoPivot", new Vector3(0f, 1.80f, 0f));
        Prim(PrimitiveType.Cube, torso, "Torso", Vector3.zero, new Vector3(0.52f, 0.95f, 0.30f), body);
        Prim(PrimitiveType.Cube, torso, "Robe", new Vector3(0f, -0.4f, -0.04f), new Vector3(0.60f, 0.7f, 0.24f), accent);
        head = Pivot(torso, "HeadPivot", new Vector3(0f, 0.62f, 0f));
        Prim(PrimitiveType.Cube, head, "Head", Vector3.zero, new Vector3(0.32f, 0.34f, 0.32f), new Color(0.85f, 0.75f, 0.6f));
        Prim(PrimitiveType.Cube, head, "Hair", new Vector3(0f, 0.22f, -0.04f), new Vector3(0.36f, 0.14f, 0.36f), new Color(0.1f, 0.05f, 0.15f));

        var armL = Pivot(torso, "Shoulder_L", new Vector3(-0.3f, 0.32f, 0f));
        Prim(PrimitiveType.Cube, armL, "Arm_L", new Vector3(0f, -0.3f, 0f), new Vector3(0.12f, 0.6f, 0.12f), body);
        var armR = Pivot(torso, "Shoulder_R", new Vector3(0.3f, 0.32f, 0f));
        Prim(PrimitiveType.Cube, armR, "Arm_R", new Vector3(0f, -0.3f, 0f), new Vector3(0.12f, 0.6f, 0.12f), body);

        var wingL = Pivot(torso, "Wing_L", new Vector3(-0.3f, 0.4f, -0.1f));
        Prim(PrimitiveType.Cube, wingL, "WingBody_L", new Vector3(-0.9f, 0.2f, 0f), new Vector3(1.8f, 0.08f, 1.3f), accent);
        var wingR = Pivot(torso, "Wing_R", new Vector3(0.3f, 0.4f, -0.1f));
        Prim(PrimitiveType.Cube, wingR, "WingBody_R", new Vector3(0.9f, 0.2f, 0f), new Vector3(1.8f, 0.08f, 1.3f), accent);

        anim.rig = LimbRig.Winged;
        anim.torso = torso; anim.head = head;
        anim.leftWing = wingL; anim.rightWing = wingR;
        anim.leftArm = armL; anim.rightArm = armR;
    }

    private static void BuildHound(Transform root, out Transform head, out Transform torso, ProceduralAnimator anim, ref Color glow, ref Color accent)
    {
        Color body = new Color(0.16f, 0.10f, 0.05f);
        accent = new Color(0.35f, 0.18f, 0.05f);
        glow = new Color(1f, 0.85f, 0.25f);

        torso = Pivot(root, "BodyPivot", new Vector3(0f, 0.72f, 0f));
        Prim(PrimitiveType.Cube, torso, "Body", Vector3.zero, new Vector3(0.55f, 0.6f, 1.5f), body);
        head = Pivot(torso, "HeadPivot", new Vector3(0f, 0.28f, 0.8f));
        Prim(PrimitiveType.Cube, head, "Head", Vector3.zero, new Vector3(0.42f, 0.42f, 0.5f), body);
        Prim(PrimitiveType.Cube, head, "Snout", new Vector3(0f, -0.08f, 0.42f), new Vector3(0.26f, 0.22f, 0.44f), body);
        Prim(PrimitiveType.Cube, head, "Ear_L", new Vector3(-0.16f, 0.28f, -0.1f), new Vector3(0.1f, 0.28f, 0.1f), body);
        Prim(PrimitiveType.Cube, head, "Ear_R", new Vector3(0.16f, 0.28f, -0.1f), new Vector3(0.1f, 0.28f, 0.1f), body);
        Prim(PrimitiveType.Cube, torso, "Tail", new Vector3(0f, 0.2f, -0.85f), new Vector3(0.1f, 0.1f, 0.7f), body);

        var fl = LegPivot(torso, "Leg_FL", new Vector3(-0.22f, -0.3f, 0.6f), body);
        var fr = LegPivot(torso, "Leg_FR", new Vector3(0.22f, -0.3f, 0.6f), body);
        var bl = LegPivot(torso, "Leg_BL", new Vector3(-0.22f, -0.3f, -0.55f), body);
        var br = LegPivot(torso, "Leg_BR", new Vector3(0.22f, -0.3f, -0.55f), body);

        anim.rig = LimbRig.Quadruped;
        anim.torso = torso; anim.head = head;
        anim.frontLegs = new[] { fl, fr };
        anim.backLegs = new[] { bl, br };
    }

    private static void BuildCrawler(Transform root, out Transform head, out Transform torso, ProceduralAnimator anim, ref Color glow, ref Color accent)
    {
        Color body = new Color(0.10f, 0.05f, 0.02f);
        accent = new Color(0.30f, 0.12f, 0.06f);
        glow = new Color(1f, 0.30f, 0.20f);

        torso = Pivot(root, "BodyPivot", new Vector3(0f, 0.55f, 0f));
        Prim(PrimitiveType.Cube, torso, "Body", Vector3.zero, new Vector3(0.6f, 0.42f, 1.1f), body);
        head = Pivot(torso, "HeadPivot", new Vector3(0f, 0.22f, 0.6f));
        Prim(PrimitiveType.Cube, head, "Head", Vector3.zero, new Vector3(0.34f, 0.3f, 0.34f), new Color(0.6f, 0.5f, 0.4f));

        var armL = Pivot(torso, "Shoulder_L", new Vector3(-0.34f, 0.1f, 0.35f));
        Prim(PrimitiveType.Cube, armL, "Arm_L", new Vector3(0f, -0.55f, 0f), new Vector3(0.13f, 1.1f, 0.13f), body);
        Prim(PrimitiveType.Cube, armL, "Claw_L", new Vector3(0f, -1.15f, 0.08f), new Vector3(0.24f, 0.12f, 0.26f), new Color(0.75f, 0.7f, 0.65f));
        var armR = Pivot(torso, "Shoulder_R", new Vector3(0.34f, 0.1f, 0.35f));
        Prim(PrimitiveType.Cube, armR, "Arm_R", new Vector3(0f, -0.55f, 0f), new Vector3(0.13f, 1.1f, 0.13f), body);
        Prim(PrimitiveType.Cube, armR, "Claw_R", new Vector3(0f, -1.15f, 0.08f), new Vector3(0.24f, 0.12f, 0.26f), new Color(0.75f, 0.7f, 0.65f));

        var legL = LegPivot(torso, "Leg_L", new Vector3(-0.2f, -0.2f, -0.4f), body);
        var legR = LegPivot(torso, "Leg_R", new Vector3(0.2f, -0.2f, -0.4f), body);

        anim.rig = LimbRig.Crawler;
        anim.torso = torso; anim.head = head;
        anim.leftArm = armL; anim.rightArm = armR;
        anim.leftUpperLeg = legL; anim.rightUpperLeg = legR;
    }

    // ================================================== humanoid primitives

    private struct Rig { public Transform lUL, rUL, lLL, rLL, lArm, rArm; }

    private static void BuildHumanoid(Transform root, Color clothing, Color skin, Color hair, bool hairLong,
                                      out Rig rig, out Transform head, out Transform torso)
    {
        var lUL = Pivot(root, "Hip_L", new Vector3(-0.11f, 0.84f, 0f));
        Prim(PrimitiveType.Cube, lUL, "UpperLeg_L", new Vector3(0f, -0.21f, 0f), new Vector3(0.17f, 0.44f, 0.17f), clothing);
        var lLL = Pivot(lUL, "Knee_L", new Vector3(0f, -0.43f, 0f));
        Prim(PrimitiveType.Cube, lLL, "LowerLeg_L", new Vector3(0f, -0.20f, 0f), new Vector3(0.15f, 0.42f, 0.15f), clothing);
        Prim(PrimitiveType.Cube, lLL, "Shoe_L", new Vector3(0f, -0.42f, 0.06f), new Vector3(0.18f, 0.12f, 0.30f), new Color(0.05f, 0.05f, 0.06f));

        var rUL = Pivot(root, "Hip_R", new Vector3(0.11f, 0.84f, 0f));
        Prim(PrimitiveType.Cube, rUL, "UpperLeg_R", new Vector3(0f, -0.21f, 0f), new Vector3(0.17f, 0.44f, 0.17f), clothing);
        var rLL = Pivot(rUL, "Knee_R", new Vector3(0f, -0.43f, 0f));
        Prim(PrimitiveType.Cube, rLL, "LowerLeg_R", new Vector3(0f, -0.20f, 0f), new Vector3(0.15f, 0.42f, 0.15f), clothing);
        Prim(PrimitiveType.Cube, rLL, "Shoe_R", new Vector3(0f, -0.42f, 0.06f), new Vector3(0.18f, 0.12f, 0.30f), new Color(0.05f, 0.05f, 0.06f));

        torso = Pivot(root, "TorsoPivot", new Vector3(0f, 0.84f, 0f));
        Prim(PrimitiveType.Cube, torso, "Torso", new Vector3(0f, 0.34f, 0f), new Vector3(0.50f, 0.68f, 0.28f), clothing);
        Prim(PrimitiveType.Cube, torso, "Hips", new Vector3(0f, 0.02f, 0f), new Vector3(0.46f, 0.2f, 0.26f), clothing);

        var lArm = Pivot(torso, "Shoulder_L", new Vector3(-0.31f, 0.6f, 0f));
        Prim(PrimitiveType.Cube, lArm, "Arm_L", new Vector3(0f, -0.3f, 0f), new Vector3(0.14f, 0.6f, 0.14f), clothing);
        Prim(PrimitiveType.Cube, lArm, "Hand_L", new Vector3(0f, -0.62f, 0f), new Vector3(0.13f, 0.14f, 0.13f), skin);
        var rArm = Pivot(torso, "Shoulder_R", new Vector3(0.31f, 0.6f, 0f));
        Prim(PrimitiveType.Cube, rArm, "Arm_R", new Vector3(0f, -0.3f, 0f), new Vector3(0.14f, 0.6f, 0.14f), clothing);
        Prim(PrimitiveType.Cube, rArm, "Hand_R", new Vector3(0f, -0.62f, 0f), new Vector3(0.13f, 0.14f, 0.13f), skin);

        head = Pivot(torso, "HeadPivot", new Vector3(0f, 0.90f, 0f));
        Prim(PrimitiveType.Cube, head, "Neck", new Vector3(0f, -0.14f, 0f), new Vector3(0.14f, 0.1f, 0.14f), skin);
        Prim(PrimitiveType.Cube, head, "Head", Vector3.zero, new Vector3(0.30f, 0.34f, 0.30f), skin);
        Prim(PrimitiveType.Cube, head, "Hair", new Vector3(0f, 0.20f, -0.02f), new Vector3(0.33f, 0.12f, 0.33f), hair);
        if (hairLong)
            Prim(PrimitiveType.Cube, head, "Hair_Back", new Vector3(0f, -0.06f, -0.17f), new Vector3(0.30f, 0.38f, 0.08f), hair);

        rig = new Rig { lUL = lUL, rUL = rUL, lLL = lLL, rLL = rLL, lArm = lArm, rArm = rArm };
    }

    private static Transform LegPivot(Transform parent, string name, Vector3 localPos, Color color)
    {
        var p = Pivot(parent, name, localPos);
        Prim(PrimitiveType.Cube, p, "LegUpper", new Vector3(0f, -0.17f, 0f), new Vector3(0.14f, 0.36f, 0.14f), color);
        var knee = Pivot(p, "Knee", new Vector3(0f, -0.34f, 0f));
        Prim(PrimitiveType.Cube, knee, "LegLower", new Vector3(0f, -0.17f, 0f), new Vector3(0.12f, 0.34f, 0.12f), color);
        Prim(PrimitiveType.Cube, knee, "Paw", new Vector3(0f, -0.36f, 0.03f), new Vector3(0.15f, 0.09f, 0.22f), new Color(0.05f, 0.05f, 0.06f));
        return p;
    }

    // ================================================================ face

    /// <summary>
    /// The single picture for a character, resolved by CharacterImageLibrary
    /// (custom image first, procedural fallback second). Never null.
    /// </summary>
    public static CharacterImage ResolveFaceImage(string survivorName, string witchName, WitchType type)
    {
        if (!string.IsNullOrEmpty(witchName))
            return CharacterImageLibrary.Get(null, witchName, type);

        if (!string.IsNullOrEmpty(survivorName))
            return CharacterImageLibrary.Get(null, survivorName, WitchType.Humanoid);

        return CharacterImageLibrary.Get(null, null, type);
    }

    /// <summary>External image first, procedural fallback. Never returns null.</summary>
    public static Texture2D ResolveFace(string survivorName, string witchName, WitchType type)
    {
        return ResolveFaceImage(survivorName, witchName, type).texture;
    }

    /// <summary>
    /// Builds the head surface. The picture is sized and placed to cover the
    /// head itself, so it reads as the whole head rather than a card stuck on
    /// the front of one. Nothing is ever drawn on top of it.
    /// </summary>
    private static Renderer AttachFace(Transform head, string survivorName, string witchName, WitchType type,
                                       Color fallbackColor)
    {
        // Every character model puts its skull cube in a child called "Head".
        Vector3 headSize = new Vector3(0.30f, 0.34f, 0.30f);
        Vector3 headCenter = Vector3.zero;
        Transform headCube = head != null ? head.Find("Head") : null;
        if (headCube != null)
        {
            headSize = headCube.localScale;
            headCenter = headCube.localPosition;
        }

        // 6% larger than the skull so no cube edge peeks around the picture.
        float width = headSize.x * 1.06f;
        float height = headSize.y * 1.06f;
        var localPos = new Vector3(headCenter.x, headCenter.y,
                                   headCenter.z + headSize.z * 0.5f + 0.006f);

        var quad = Prim(PrimitiveType.Quad, head, "Face", localPos, new Vector3(width, height, 1f), null);
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // normal -> +Z (character forward)

        var sh = Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_Color")) mat.color = fallbackColor;

        var r = quad.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Never embed a runtime Texture2D in the saved scene (corrupts the built
        // level). Edit time = flat colour; runtime = image / procedural default.
        if (Application.isPlaying)
        {
            CharacterImage image = ResolveFaceImage(survivorName, witchName, type);
            string label = !string.IsNullOrEmpty(witchName) ? witchName : survivorName;
            ApplyFaceTexture(r, mat, image, label);

            // A photograph replaces the whole head, so the modelled muzzle would
            // poke through it. It is removed, never drawn over the picture.
            if (!image.isFallback) HideMuzzle(head);
        }
        return r;
    }

    /// <summary>Removes modelled nose/mouth geometry from a photograph head.</summary>
    private static void HideMuzzle(Transform head)
    {
        if (head == null) return;
        var snout = head.Find("Snout");
        if (snout == null) return;
        var renderer = snout.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
    }

    /// <summary>
    /// Puts a resolved picture on a face quad: texture, cover-crop UVs (the quad
    /// is turned 180 degrees, which mirrors the image, so X is flipped back) and
    /// one diagnostic line per character. Nothing is layered on top.
    /// </summary>
    private static void ApplyFaceTexture(Renderer renderer, Material mat, CharacterImage image, string characterLabel)
    {
        if (renderer == null || mat == null || !image.IsValid) return;

        Vector3 quadSize = renderer.transform.localScale;
        float quadAspect = (quadSize.x > 0.0001f && quadSize.y > 0.0001f)
            ? quadSize.x / quadSize.y
            : 1f;

        if (mat.HasProperty("_MainTex"))
        {
            mat.mainTexture = image.texture;

            // A generated fallback is drawn square for this exact quad, so it
            // fills it instead of being cropped like a real photograph.
            float sourceAspect = image.isFallback ? quadAspect : image.Aspect;
            CharacterImageLibrary.ApplyFaceUv(mat, sourceAspect, quadAspect, false);
        }
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", image.texture); // URP safety

        CharacterImageLibrary.LogApplied("gameplay texture", characterLabel);
    }

    /// <summary>Re-apply the face at runtime (bootstrap + character select).</summary>
    public static void ApplyFace(CharacterVisual visual)
    {
        if (visual == null || visual.faceRenderer == null) return;
        var mat = visual.faceRenderer.sharedMaterial;
        if (mat == null || !mat.HasProperty("_MainTex"))
        {
            var sh = Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            mat = new Material(sh);
            visual.faceRenderer.sharedMaterial = mat;
        }
        WitchType type = visual.isChaser ? WitchTypeFromName(visual.displayName) : WitchType.Humanoid;
        var image = ResolveFaceImage(visual.isChaser ? null : visual.displayName,
                                     visual.isChaser ? visual.displayName : null, type);
        ApplyFaceTexture(visual.faceRenderer, mat, image, visual.displayName);
        visual.faceMaterial = mat;
    }

    public static WitchType WitchTypeFromName(string name)
    {
        // OfficialName() accepts the current name and any previous one, so older
        // serialized assets and saved sessions still resolve to the right witch.
        switch (CharacterLore.OfficialName(name))
        {
            case "Demonisa Belle": return WitchType.Winged;
            case "Kyla Ni-Garcia": return WitchType.Hound;
            case "Jannak":         return WitchType.Crawler;
            case "Sandy Vill":     return WitchType.Tall;
            case "Beamoth":        return WitchType.Final;
            default:               return WitchType.Humanoid;
        }
    }

    // ============================================================== helpers

    private static Transform Pivot(Transform parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Color? color)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        StripCollider(obj);
        if (color.HasValue) obj.GetComponent<Renderer>().sharedMaterial = Surface(color.Value);
        return obj;
    }

    private static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    private static void AddCapsuleCollider(GameObject root, float height, float radius)
    {
        var cap = root.AddComponent<CapsuleCollider>();
        cap.height = height; cap.radius = radius; cap.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private static System.Collections.Generic.Dictionary<string, Material> _cache =
        new System.Collections.Generic.Dictionary<string, Material>();

    public static Material Surface(Color c, float emission = 0.14f)
    {
        string key = "s|" + c.r + "_" + c.g + "_" + c.b + "_" + emission;
        Material m;
        if (_cache.TryGetValue(key, out m)) return m;
        m = Emissive(c, emission, false);
        _cache[key] = m;
        return m;
    }

    public static Material Emissive(Color c, float intensity, bool unlit)
    {
        Shader sh = unlit ? Shader.Find("Unlit/Color") : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        var m = new Material(sh);
        if (m.HasProperty("_Color")) m.color = c;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        if (!unlit && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return m;
    }
}
