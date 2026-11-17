using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstGame
{
    /// <summary>
    /// Prevents legacy world-object clicks from leaking through an EventSystem UI hit.
    /// </summary>
    public static class UiInputGuard
    {
        public static bool IsPointerOverUi()
        {
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return false;

            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (eventSystem.IsPointerOverGameObject(touch.fingerId)) return true;
            }

            return eventSystem.IsPointerOverGameObject();
        }
    }
}
