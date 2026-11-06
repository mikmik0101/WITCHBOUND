using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputDiagnostics : MonoBehaviour
{
    private EventSystem eventSystem;
    private Canvas canvas;
    private Vector2 lastMousePos;
    private bool lastLeftHeld;

    private void Start()
    {
        eventSystem = FindObjectOfType<EventSystem>();
        canvas = FindObjectOfType<Canvas>();

        Debug.Log("[INPUT DIAGNOSTICS] === INITIALIZATION ===");
        Debug.Log("[INPUT DIAGNOSTICS] EventSystem found: " + (eventSystem != null));
        if (eventSystem != null)
        {
            Debug.Log("[INPUT DIAGNOSTICS] EventSystem.enabled: " + eventSystem.enabled);
            Debug.Log("[INPUT DIAGNOSTICS] EventSystem.activeInHierarchy: " + eventSystem.gameObject.activeInHierarchy);
            var inputModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (var module in inputModules)
            {
                Debug.Log("[INPUT DIAGNOSTICS] InputModule: " + module.GetType().Name + " (enabled=" + module.enabled + ")");
            }
        }

        if (canvas != null)
        {
            Debug.Log("[INPUT DIAGNOSTICS] Canvas.enabled: " + canvas.enabled);
            Debug.Log("[INPUT DIAGNOSTICS] Canvas.renderMode: " + canvas.renderMode);
        }

        var buttons = FindObjectsOfType<Button>();
        foreach (var button in buttons)
        {
            if (button.gameObject.activeInHierarchy)
            {
                var image = button.GetComponent<Image>();
                Debug.Log("[INPUT DIAGNOSTICS] Button '" + button.name + "': interactable=" + button.interactable + ", raycastTarget=" + (image != null ? image.raycastTarget.ToString() : "no-image"));
            }
        }
        Debug.Log("[INPUT DIAGNOSTICS] === END INITIALIZATION ===");
    }

    private void Update()
    {
        if (InputStateManager.Instance != null && !InputStateManager.Instance.IsMenuMode())
            return;

        if (eventSystem == null)
            return;

        // Log only on mouse move (throttled)
        Vector2 mousePos = Input.mousePosition;
        if ((mousePos - lastMousePos).sqrMagnitude > 4f)
        {
            Debug.Log("[INPUT DIAGNOSTICS] Mouse moved to " + mousePos);

            // Raycast check on move
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = mousePos;
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);
            if (raycastResults.Count > 0)
            {
                Debug.Log("[INPUT DIAGNOSTICS] Raycast hit: " + raycastResults[0].gameObject.name);
            }
            lastMousePos = mousePos;
        }

        // Log only on click down/up
        bool leftDown = Input.GetMouseButtonDown(0);
        bool leftUp = Input.GetMouseButtonUp(0);
        bool leftHeld = Input.GetMouseButton(0);

        if (leftDown)
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = mousePos;
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);
            Debug.Log("[INPUT DIAGNOSTICS] CLICK at " + mousePos + " -> " + (raycastResults.Count > 0 ? raycastResults[0].gameObject.name : "NO HIT"));
        }

        if (leftUp && lastLeftHeld)
        {
            Debug.Log("[INPUT DIAGNOSTICS] CLICK RELEASED");
        }
        lastLeftHeld = leftHeld;
    }
}
