using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDiagnostics : MonoBehaviour
{
    private void Start()
    {
        InvokeRepeating("LogDiagnostics", 1f, 5f);
    }
    
    private void LogDiagnostics()
    {
        var es = EventSystem.current;
        if (es != null)
        {
            Debug.Log($"[UIDiagnostics] EventSystem: {es.name}, enabled: {es.enabled}, module: {es.currentInputModule?.GetType().Name}");
        }
        else
        {
            Debug.LogError("[UIDiagnostics] No EventSystem found!");
        }
        
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            var gr = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"[UIDiagnostics] Canvas: {canvas.name}, renderMode: {canvas.renderMode}, raycaster: {(gr != null ? "present" : "MISSING")}");
            
            // Count buttons
            var buttons = canvas.GetComponentsInChildren<Button>();
            Debug.Log($"[UIDiagnostics] Button count: {buttons.Length}");
            foreach (var btn in buttons)
            {
                var rect = btn.GetComponent<RectTransform>();
                Debug.Log($"[UIDiagnostics] Button: {btn.name}, interactable: {btn.interactable}, active: {btn.gameObject.activeInHierarchy}, rect: {rect.rect}");
            }
        }
        
        // Check for blocking panels
        var images = FindObjectsOfType<Image>();
        int raycastCount = 0;
        foreach (var img in images)
        {
            if (img.raycastTarget)
                raycastCount++;
        }
        Debug.Log($"[UIDiagnostics] Total Images: {images.Length}, RaycastTarget=true: {raycastCount}");
    }
}
