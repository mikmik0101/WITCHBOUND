using UnityEngine;

public static class CharacterModelBuilder
{
    public static GameObject BuildSurvivor(SurvivorData data, Vector3 position)
    {
        var root = new GameObject($"Survivor_{data.survivorName}");
        root.transform.position = position;
        
        var torso = CreatePart("Torso", PrimitiveType.Cube, root.transform, new Vector3(0, 1f, 0), new Vector3(0.6f, 0.8f, 0.3f));
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 1.7f, 0), new Vector3(0.4f, 0.4f, 0.4f));
        var leftArm = CreatePart("LeftArm", PrimitiveType.Cube, root.transform, new Vector3(-0.4f, 1.1f, 0), new Vector3(0.15f, 0.6f, 0.15f));
        var rightArm = CreatePart("RightArm", PrimitiveType.Cube, root.transform, new Vector3(0.4f, 1.1f, 0), new Vector3(0.15f, 0.6f, 0.15f));
        var leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, root.transform, new Vector3(-0.15f, 0.3f, 0), new Vector3(0.18f, 0.6f, 0.18f));
        var rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, root.transform, new Vector3(0.15f, 0.3f, 0), new Vector3(0.18f, 0.6f, 0.18f));
        
        var hair = CreatePart("Hair", PrimitiveType.Sphere, head.transform, new Vector3(0, 0.15f, 0), new Vector3(1.2f, 0.8f, 1.2f));
        hair.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
        
        Color bodyColor = GetSurvivorBodyColor(data.survivorName);
        Color hairColor = GetSurvivorHairColor(data.survivorName);
        
        ApplyMaterial(torso, bodyColor);
        ApplyMaterial(head, new Color(0.9f, 0.75f, 0.65f));
        ApplyMaterial(leftArm, bodyColor);
        ApplyMaterial(rightArm, bodyColor);
        ApplyMaterial(leftLeg, bodyColor);
        ApplyMaterial(rightLeg, bodyColor);
        ApplyMaterial(hair, hairColor);
        
        var leftShoe = CreatePart("LeftShoe", PrimitiveType.Cube, leftLeg.transform, new Vector3(0, -0.35f, 0.1f), new Vector3(1.2f, 0.3f, 1.5f));
        var rightShoe = CreatePart("RightShoe", PrimitiveType.Cube, rightLeg.transform, new Vector3(0, -0.35f, 0.1f), new Vector3(1.2f, 0.3f, 1.5f));
        ApplyMaterial(leftShoe, Color.black);
        ApplyMaterial(rightShoe, Color.black);
        
        return root;
    }
    
    public static GameObject BuildWitch(WitchData data, Vector3 position)
    {
        var root = new GameObject($"Witch_{data.witchName}");
        root.transform.position = position;
        
        switch (data.witchType)
        {
            case WitchType.Humanoid:
                BuildHumanoidWitch(root, data);
                break;
            case WitchType.Winged:
                BuildWingedWitch(root, data);
                break;
            case WitchType.Hound:
                BuildHoundWitch(root, data);
                break;
            case WitchType.Crawler:
                BuildCrawlerWitch(root, data);
                break;
            case WitchType.Tall:
                BuildTallWitch(root, data);
                break;
            case WitchType.Final:
                BuildFinalWitch(root, data);
                break;
        }
        
        return root;
    }

    /// <summary>
    /// Enlarges a witch's head to improve face readability from gameplay
    /// distance, while keeping it attached to the torso/body so it never
    /// floats, clips or leaves a gap. The face quad is a child of the head, so
    /// the photograph scales and re-centres with it automatically.
    /// </summary>
    private const float WITCH_HEAD_SCALE = 1.25f;

    private static void ScaleWitchHead(GameObject head, GameObject body, float overlap = 0.06f)
    {
        if (head == null || body == null) return;

        var s = head.transform.localScale;
        head.transform.localScale = new Vector3(
            s.x * WITCH_HEAD_SCALE, s.y * WITCH_HEAD_SCALE, s.z * WITCH_HEAD_SCALE);

        // Sphere primitive radius is half its local scale; place the head so its
        // bottom sits just inside the torso/body top -> no gap, no floating.
        float bodyTop = body.transform.localPosition.y + 0.5f * body.transform.localScale.y;
        float radius = 0.5f * head.transform.localScale.y;
        head.transform.localPosition = new Vector3(
            head.transform.localPosition.x,
            bodyTop - overlap + radius,
            head.transform.localPosition.z);
    }

    private static void BuildHumanoidWitch(GameObject root, WitchData data)
    {
        var torso = CreatePart("Torso", PrimitiveType.Cube, root.transform, new Vector3(0, 1.2f, 0), new Vector3(0.5f, 1f, 0.3f));
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 2.1f, 0), new Vector3(0.35f, 0.35f, 0.35f));
        var leftArm = CreatePart("LeftArm", PrimitiveType.Cube, root.transform, new Vector3(-0.35f, 1.3f, 0), new Vector3(0.12f, 0.8f, 0.12f));
        var rightArm = CreatePart("RightArm", PrimitiveType.Cube, root.transform, new Vector3(0.35f, 1.3f, 0), new Vector3(0.12f, 0.8f, 0.12f));
        var leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, root.transform, new Vector3(-0.12f, 0.4f, 0), new Vector3(0.15f, 0.8f, 0.15f));
        var rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, root.transform, new Vector3(0.12f, 0.4f, 0), new Vector3(0.15f, 0.8f, 0.15f));
        
        var hair = CreatePart("Hair", PrimitiveType.Sphere, head.transform, new Vector3(0, 0.2f, 0), new Vector3(1.5f, 1f, 1.5f));
        hair.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);
        
        var cloak = CreatePart("Cloak", PrimitiveType.Cube, root.transform, new Vector3(0, 0.8f, -0.2f), new Vector3(0.8f, 1.2f, 0.1f));
        
        Color witchColor = new Color(0.05f, 0.02f, 0.08f);
        ApplyMaterial(torso, witchColor);
        ApplyMaterial(head, new Color(0.8f, 0.7f, 0.6f));
        ApplyMaterial(leftArm, witchColor);
        ApplyMaterial(rightArm, witchColor);
        ApplyMaterial(leftLeg, witchColor);
        ApplyMaterial(rightLeg, witchColor);
        ApplyMaterial(hair, new Color(0.1f, 0.05f, 0.15f));
        ApplyMaterial(cloak, new Color(0.15f, 0.05f, 0.2f));

        ScaleWitchHead(head, torso);
    }
    
    private static void BuildWingedWitch(GameObject root, WitchData data)
    {
        var torso = CreatePart("Torso", PrimitiveType.Cube, root.transform, new Vector3(0, 1.5f, 0), new Vector3(0.5f, 1f, 0.3f));
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 2.3f, 0), new Vector3(0.35f, 0.35f, 0.35f));
        
        var leftWing = CreatePart("LeftWing", PrimitiveType.Cube, root.transform, new Vector3(-1.5f, 1.8f, -0.5f), new Vector3(2.5f, 0.1f, 1.5f));
        var rightWing = CreatePart("RightWing", PrimitiveType.Cube, root.transform, new Vector3(1.5f, 1.8f, -0.5f), new Vector3(2.5f, 0.1f, 1.5f));
        
        var leftMembrane = CreatePart("LeftMembrane", PrimitiveType.Plane, leftWing.transform, new Vector3(-0.5f, 0, 0), new Vector3(2f, 1f, 2f));
        var rightMembrane = CreatePart("RightMembrane", PrimitiveType.Plane, rightWing.transform, new Vector3(0.5f, 0, 0), new Vector3(2f, 1f, 2f));
        
        Color witchColor = new Color(0.1f, 0.02f, 0.15f);
        ApplyMaterial(torso, witchColor);
        ApplyMaterial(head, new Color(0.85f, 0.75f, 0.6f));
        ApplyMaterial(leftWing, new Color(0.2f, 0.05f, 0.3f));
        ApplyMaterial(rightWing, new Color(0.2f, 0.05f, 0.3f));
        ApplyMaterial(leftMembrane, new Color(0.3f, 0.1f, 0.4f, 0.8f));
        ApplyMaterial(rightMembrane, new Color(0.3f, 0.1f, 0.4f, 0.8f));

        ScaleWitchHead(head, torso);
    }
    
    private static void BuildHoundWitch(GameObject root, WitchData data)
    {
        var body = CreatePart("Body", PrimitiveType.Cube, root.transform, new Vector3(0, 0.8f, 0), new Vector3(1.2f, 0.6f, 0.6f));
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 1.2f, 0.6f), new Vector3(0.5f, 0.4f, 0.6f));
        var snout = CreatePart("Snout", PrimitiveType.Cube, head.transform, new Vector3(0, -0.1f, 0.4f), new Vector3(0.3f, 0.2f, 0.5f));
        
        var fl = CreatePart("FrontLeft", PrimitiveType.Cube, root.transform, new Vector3(-0.4f, 0.2f, 0.3f), new Vector3(0.15f, 0.6f, 0.15f));
        var fr = CreatePart("FrontRight", PrimitiveType.Cube, root.transform, new Vector3(0.4f, 0.2f, 0.3f), new Vector3(0.15f, 0.6f, 0.15f));
        var bl = CreatePart("BackLeft", PrimitiveType.Cube, root.transform, new Vector3(-0.4f, 0.2f, -0.3f), new Vector3(0.15f, 0.6f, 0.15f));
        var br = CreatePart("BackRight", PrimitiveType.Cube, root.transform, new Vector3(0.4f, 0.2f, -0.3f), new Vector3(0.15f, 0.6f, 0.15f));
        
        var tail = CreatePart("Tail", PrimitiveType.Cube, root.transform, new Vector3(0, 0.9f, -0.5f), new Vector3(0.1f, 0.1f, 0.6f));
        tail.transform.rotation = Quaternion.Euler(-30, 0, 0);
        
        var leftEar = CreatePart("LeftEar", PrimitiveType.Cube, head.transform, new Vector3(-0.2f, 0.25f, -0.1f), new Vector3(0.1f, 0.3f, 0.1f));
        var rightEar = CreatePart("RightEar", PrimitiveType.Cube, head.transform, new Vector3(0.2f, 0.25f, -0.1f), new Vector3(0.1f, 0.3f, 0.1f));
        
        Color houndColor = new Color(0.15f, 0.1f, 0.05f);
        ApplyMaterial(body, houndColor);
        ApplyMaterial(head, houndColor);
        ApplyMaterial(snout, houndColor);
        ApplyMaterial(fl, houndColor);
        ApplyMaterial(fr, houndColor);
        ApplyMaterial(bl, houndColor);
        ApplyMaterial(br, houndColor);
        ApplyMaterial(tail, houndColor);
        ApplyMaterial(leftEar, houndColor);
        ApplyMaterial(rightEar, houndColor);

        ScaleWitchHead(head, body);
    }
    
    private static void BuildCrawlerWitch(GameObject root, WitchData data)
    {
        var body = CreatePart("Body", PrimitiveType.Cube, root.transform, new Vector3(0, 0.4f, 0), new Vector3(1.2f, 0.3f, 0.8f));
        
        var leftArm = CreatePart("LeftArm", PrimitiveType.Cube, root.transform, new Vector3(-0.7f, 0.3f, 0.2f), new Vector3(0.15f, 0.6f, 0.15f));
        var rightArm = CreatePart("RightArm", PrimitiveType.Cube, root.transform, new Vector3(0.7f, 0.3f, 0.2f), new Vector3(0.15f, 0.6f, 0.15f));
        
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 0.5f, 0.4f), new Vector3(0.3f, 0.25f, 0.3f));
        
        var leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, root.transform, new Vector3(-0.3f, 0.1f, -0.2f), new Vector3(0.1f, 0.2f, 0.1f));
        var rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, root.transform, new Vector3(0.3f, 0.1f, -0.2f), new Vector3(0.1f, 0.2f, 0.1f));
        
        var leftClaw = CreatePart("LeftClaw", PrimitiveType.Cube, leftArm.transform, new Vector3(0, -0.35f, 0.1f), new Vector3(0.3f, 0.1f, 0.3f));
        var rightClaw = CreatePart("RightClaw", PrimitiveType.Cube, rightArm.transform, new Vector3(0, -0.35f, 0.1f), new Vector3(0.3f, 0.1f, 0.3f));
        
        Color crawlerColor = new Color(0.1f, 0.05f, 0.02f);
        ApplyMaterial(body, crawlerColor);
        ApplyMaterial(leftArm, crawlerColor);
        ApplyMaterial(rightArm, crawlerColor);
        ApplyMaterial(head, new Color(0.7f, 0.6f, 0.5f));
        ApplyMaterial(leftLeg, crawlerColor);
        ApplyMaterial(rightLeg, crawlerColor);
        ApplyMaterial(leftClaw, new Color(0.3f, 0.2f, 0.1f));
        ApplyMaterial(rightClaw, new Color(0.3f, 0.2f, 0.1f));

        ScaleWitchHead(head, body);
    }
    
    private static void BuildTallWitch(GameObject root, WitchData data)
    {
        var torso = CreatePart("Torso", PrimitiveType.Cube, root.transform, new Vector3(0, 2.5f, 0), new Vector3(0.6f, 2.5f, 0.3f));
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 4.2f, 0), new Vector3(0.4f, 0.4f, 0.4f));
        
        var leftArm = CreatePart("LeftArm", PrimitiveType.Cube, root.transform, new Vector3(-0.5f, 2.5f, 0), new Vector3(0.15f, 2f, 0.15f));
        var rightArm = CreatePart("RightArm", PrimitiveType.Cube, root.transform, new Vector3(0.5f, 2.5f, 0), new Vector3(0.15f, 2f, 0.15f));
        
        var leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, root.transform, new Vector3(-0.15f, 0.5f, 0), new Vector3(0.2f, 2f, 0.2f));
        var rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, root.transform, new Vector3(0.15f, 0.5f, 0), new Vector3(0.2f, 2f, 0.2f));
        
        var leftHand = CreatePart("LeftHand", PrimitiveType.Sphere, leftArm.transform, new Vector3(0, -1.1f, 0), new Vector3(0.4f, 0.4f, 0.4f));
        var rightHand = CreatePart("RightHand", PrimitiveType.Sphere, rightArm.transform, new Vector3(0, -1.1f, 0), new Vector3(0.4f, 0.4f, 0.4f));
        
        var hair = CreatePart("Hair", PrimitiveType.Sphere, head.transform, new Vector3(0, 0.2f, 0), new Vector3(1.5f, 1f, 1.5f));
        hair.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
        
        Color tallColor = new Color(0.08f, 0.05f, 0.12f);
        ApplyMaterial(torso, tallColor);
        ApplyMaterial(head, new Color(0.9f, 0.8f, 0.7f));
        ApplyMaterial(leftArm, tallColor);
        ApplyMaterial(rightArm, tallColor);
        ApplyMaterial(leftLeg, tallColor);
        ApplyMaterial(rightLeg, tallColor);
        ApplyMaterial(leftHand, tallColor);
        ApplyMaterial(rightHand, tallColor);
        ApplyMaterial(hair, new Color(0.2f, 0.1f, 0.3f));

        ScaleWitchHead(head, torso);
    }

    private static void BuildFinalWitch(GameObject root, WitchData data)
    {
        var torso = CreatePart("Torso", PrimitiveType.Cube, root.transform, new Vector3(0, 1.5f, 0), new Vector3(0.6f, 1.2f, 0.3f));
        var head = CreatePart("Head", PrimitiveType.Sphere, root.transform, new Vector3(0, 2.5f, 0), new Vector3(0.4f, 0.4f, 0.4f));
        
        var leftArm = CreatePart("LeftArm", PrimitiveType.Cube, root.transform, new Vector3(-0.4f, 1.6f, 0), new Vector3(0.15f, 1f, 0.15f));
        var rightArm = CreatePart("RightArm", PrimitiveType.Cube, root.transform, new Vector3(0.4f, 1.6f, 0), new Vector3(0.15f, 1f, 0.15f));
        
        var leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, root.transform, new Vector3(-0.15f, 0.5f, 0), new Vector3(0.18f, 1f, 0.18f));
        var rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, root.transform, new Vector3(0.15f, 0.5f, 0), new Vector3(0.18f, 1f, 0.18f));
        
        var aura = CreatePart("Aura", PrimitiveType.Sphere, root.transform, new Vector3(0, 1.5f, 0), new Vector3(3f, 3f, 3f));
        aura.transform.localScale = new Vector3(3f, 3f, 3f);
        
        var leftClaw = CreatePart("LeftClaw", PrimitiveType.Cube, leftArm.transform, new Vector3(0, -0.55f, 0.1f), new Vector3(0.3f, 0.15f, 0.3f));
        var rightClaw = CreatePart("RightClaw", PrimitiveType.Cube, rightArm.transform, new Vector3(0, -0.55f, 0.1f), new Vector3(0.3f, 0.15f, 0.3f));
        
        Color finalColor = new Color(0.15f, 0.02f, 0.2f);
        Color auraColor = new Color(0.5f, 0.1f, 0.8f, 0.3f);
        
        ApplyMaterial(torso, finalColor);
        ApplyMaterial(head, new Color(0.8f, 0.7f, 0.6f));
        ApplyMaterial(leftArm, finalColor);
        ApplyMaterial(rightArm, finalColor);
        ApplyMaterial(leftLeg, finalColor);
        ApplyMaterial(rightLeg, finalColor);
        ApplyMaterial(leftClaw, new Color(0.4f, 0.1f, 0.5f));
        ApplyMaterial(rightClaw, new Color(0.4f, 0.1f, 0.5f));
        
        var auraMat = new Material(Shader.Find("Standard"));
        auraMat.color = auraColor;
        auraMat.EnableKeyword("_EMISSION");
        auraMat.SetColor("_EmissionColor", new Color(0.5f, 0.1f, 0.8f) * 0.5f);
        auraMat.SetFloat("_Mode", 3);
        auraMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        auraMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        auraMat.SetInt("_ZWrite", 0);
        auraMat.renderQueue = 3000;
        aura.GetComponent<Renderer>().material = auraMat;

        ScaleWitchHead(head, torso);
    }
    
    private static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;
        return obj;
    }
    
    private static void ApplyMaterial(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }
    }
    
    private static Color GetSurvivorBodyColor(string name)
    {
        switch (CharacterLore.OfficialName(name))
        {
            case "Luis Padilla": return new Color(0.2f, 0.3f, 0.5f);
            case "Mick Richards": return new Color(0.3f, 0.4f, 0.2f);
            case "Josh Garcia": return new Color(0.5f, 0.3f, 0.2f);
            case "Vincent Reid": return new Color(0.4f, 0.2f, 0.4f);
            default: return Color.gray;
        }
    }
    
    private static Color GetSurvivorHairColor(string name)
    {
        switch (CharacterLore.OfficialName(name))
        {
            case "Luis Padilla": return new Color(0.3f, 0.2f, 0.1f);
            case "Mick Richards": return new Color(0.1f, 0.1f, 0.1f);
            case "Josh Garcia": return new Color(0.6f, 0.4f, 0.2f);
            case "Vincent Reid": return new Color(0.15f, 0.1f, 0.05f);
            default: return Color.black;
        }
    }
}
