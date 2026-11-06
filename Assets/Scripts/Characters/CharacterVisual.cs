using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marker + metadata attached to every runtime-built character model.
/// Must live in its own file (name must match the class) so Unity can create a
/// valid MonoScript for it - otherwise saving it into a scene corrupts the level.
/// </summary>
public class CharacterVisual : MonoBehaviour
{
    public string displayName = "";
    public bool isChaser = false;
    public Transform headTransform;
    public Renderer faceRenderer;
    public Material faceMaterial;

    /// <summary>
    /// Every enabled character model in the scene. CharacterNameTags reads this
    /// instead of scanning the scene each frame, and registration is per
    /// instance, so a rebuilt companion roster can never leave duplicate tags.
    /// </summary>
    public static readonly List<CharacterVisual> Active = new List<CharacterVisual>();

    private void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }
}
