using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개별 채팅 메시지 UI 컴포넌트
    /// </summary>
    public class ChatMessageUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text timestampText;
        [SerializeField] private Image backgroundImage;
        
        [Header("Colors")]
        [SerializeField] private Color defaultBackgroundColor = new Color(0, 0, 0, 0.3f);
        [SerializeField] private Color highlightBackgroundColor = new Color(1, 1, 0, 0.2f);
        
        /// <summary>
        /// 메시지 설정
        /// </summary>
        public void SetMessage(ChatMessage chatMessage, Color textColor)
        {
            // 플레이어 이름
            if (playerNameText != null)
            {
                playerNameText.text = chatMessage.senderName;
                playerNameText.color = textColor;
            }
            
            // 메시지 내용
            if (messageText != null)
            {
                messageText.text = chatMessage.message;
                messageText.color = textColor;
            }
            
            // 타임스탬프
            if (timestampText != null)
            {
                timestampText.text = chatMessage.timestamp.ToString("HH:mm");
                timestampText.color = Color.gray;
            }
            
            // 배경 색상
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultBackgroundColor;
            }
        }
        
        /// <summary>
        /// 하이라이트 설정
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlight ? highlightBackgroundColor : defaultBackgroundColor;
            }
        }
    }
}