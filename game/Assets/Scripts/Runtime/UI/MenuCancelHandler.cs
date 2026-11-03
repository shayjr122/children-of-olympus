using UnityEngine;
using UnityEngine.EventSystems;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// Sits on each pause-menu item: the UI Cancel action (gamepad B / East, keyboard Esc) sent by the EventSystem to
    /// the selected item closes the menu, like Resume. Uses the UI event path, so it never touches InputAction (AD-7).
    /// </summary>
    public sealed class MenuCancelHandler : MonoBehaviour, ICancelHandler
    {
        [SerializeField] PauseController _pause;

        public void OnCancel(BaseEventData eventData)
        {
            if (_pause != null) _pause.CloseFromCancel();
        }
    }
}
