using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

/**
 * Skripta koja hvata i proslije?uje pointer eventove.
 * Ovo je modificirana verzija koja omogu?ava postavljanje FocusRequired atributa.
 */
public class CustomPointerHandler : BaseInputHandler, IMixedRealityPointerHandler
{
    [SerializeField]
    [Tooltip("Whether input events should be marked as used after handling so other handlers in the same game object ignore them")]
    private bool MarkEventsAsUsed = false;

    /// <summary>
    /// Unity event raised on pointer down.
    /// </summary>
    public PointerUnityEvent OnPointerDown = new PointerUnityEvent();

    /// <summary>
    /// Unity event raised on pointer up.
    /// </summary>
    public PointerUnityEvent OnPointerUp = new PointerUnityEvent();

    /// <summary>
    /// Unity event raised on pointer clicked.
    /// </summary>
    public PointerUnityEvent OnPointerClicked = new PointerUnityEvent();

    /// <summary>
    /// Unity event raised every frame the pointer is down.
    /// </summary>
    public PointerUnityEvent OnPointerDragged = new PointerUnityEvent();

    public void SetFocusRequired(bool value)
    {
        this.IsFocusRequired = value;
    }

    protected override void RegisterHandlers()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }

    protected override void UnregisterHandlers()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }

    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (!eventData.used)
        {
            OnPointerDown.Invoke(eventData);
            if (MarkEventsAsUsed)
            {
                eventData.Use();
            }
        }
    }

    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (!eventData.used)
        {
            OnPointerUp.Invoke(eventData);
            if (MarkEventsAsUsed)
            {
                eventData.Use();
            }
        }
    }
    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        if (!eventData.used)
        {
            OnPointerClicked.Invoke(eventData);
            if (MarkEventsAsUsed)
            {
                eventData.Use();
            }
        }
    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (!eventData.used)
        {
            OnPointerDragged.Invoke(eventData);
            if (MarkEventsAsUsed)
            {
                eventData.Use();
            }
        }
    }
}
