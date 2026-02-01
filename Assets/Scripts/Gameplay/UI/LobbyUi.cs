using Gameplay.Leaderboard;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.FPSSample_2
{
    [RequireComponent(typeof(UIDocument))]
    public class LobbyUi : MonoBehaviour
    {
        VisualElement m_RootElement;
        Label m_HostLabel;
        Label m_ClientLabel;

        void OnEnable()
        {
            m_RootElement = GetComponent<UIDocument>().rootVisualElement;
            m_HostLabel = m_RootElement.Q<Label>("host-label");
            m_ClientLabel = m_RootElement.Q<Label>("client-label");
        }

        void LateUpdate()
        {
            bool isInLobby = GameSettings.Instance.GameState == GlobalGameState.Lobby;
            m_RootElement.style.display = isInLobby ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isInLobby)
                return;

            bool isHost = GameSettings.Instance.IsHost;

            m_HostLabel.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            m_ClientLabel.style.display = isHost ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
