using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Contournement d'un bug uGUI 2.0 (Unity 6) : en build plein ecran, Slider.UpdateDrag passe par
// MultipleDisplayUtilities.GetRelativeMousePositionForDrag, qui compare resolution de rendu et
// resolution systeme. Sur WebGL (rendu a devicePixelRatio), elles different, l'index d'ecran
// recalcule ne correspond plus et le drag est ignore en silence. Le raycast (surbrillance) n'est
// pas concerne. Ce composant recoit les memes evenements pointeur que le Slider et recalcule la
// valeur a partir de eventData.position, comme le fait l'editeur.
// A retirer quand le package com.unity.ugui corrige le chemin WebGL.
[RequireComponent(typeof(Slider))]
public class SliderFullscreenDragFix : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    Slider _slider;
    Vector2 _offset;

    void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!MayDrag(eventData)) return;

        _offset = Vector2.zero;
        var handle = _slider.handleRect;
        if (handle != null && RectTransformUtility.RectangleContainsScreenPoint(handle, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
        {
            // Clic sur le handle : on memorise l'offset pour qu'il ne saute pas sous le curseur.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(handle, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out _offset);
        }
        else
        {
            UpdateDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!MayDrag(eventData)) return;
        UpdateDrag(eventData);
    }

    bool MayDrag(PointerEventData eventData)
    {
        return _slider.IsActive() && _slider.IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
    }

    void UpdateDrag(PointerEventData eventData)
    {
        var container = _slider.handleRect != null ? _slider.handleRect.parent as RectTransform
                      : _slider.fillRect != null ? _slider.fillRect.parent as RectTransform
                      : null;
        if (container == null) return;

        bool horizontal = _slider.direction == Slider.Direction.LeftToRight || _slider.direction == Slider.Direction.RightToLeft;
        bool reverse = _slider.direction == Slider.Direction.RightToLeft || _slider.direction == Slider.Direction.TopToBottom;
        int axis = horizontal ? 0 : 1;

        float size = container.rect.size[axis];
        if (size <= 0f) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, eventData.position, eventData.pressEventCamera, out var localCursor))
            return;
        localCursor -= container.rect.position;

        float val = Mathf.Clamp01((localCursor - _offset)[axis] / size);
        _slider.normalizedValue = reverse ? 1f - val : val;
    }
}
