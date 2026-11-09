
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileMoveButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    public enum MobileDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Mobile Button")]
    public player targetPlayer;
    public MobileDirection direction;

    // 버튼을 누르면 이동 시작
    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetPlayer != null)
        {
            targetPlayer.SetMobileDirection(direction, true);
        }
    }

    // 버튼에서 손가락을 떼면 이동 정지
    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseButton();
    }

    // 손가락이 버튼 영역 밖으로 나가도 이동 정지
    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseButton();
    }

    private void ReleaseButton()
    {
        if (targetPlayer != null)
        {
            targetPlayer.SetMobileDirection(direction, false);
        }
    }
}
