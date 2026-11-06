using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickDiagnostics : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private Button button;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => Debug.Log($"[CLICK DIAG] {gameObject.name} Button.onClick FIRED!"));
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[CLICK DIAG] {gameObject.name} PointerEnter");
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[CLICK DIAG] {gameObject.name} PointerExit");
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[CLICK DIAG] {gameObject.name} PointerDown - button: {eventData.button}");
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[CLICK DIAG] {gameObject.name} PointerUp - button: {eventData.button}");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[CLICK DIAG] {gameObject.name} PointerClick - button: {eventData.button}");
    }
}
