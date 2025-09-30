using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _joinCodeInputField;

    private void Start()
    {
        _startButton.onClick.AddListener(OnStartButtonClicked);
        _joinButton.onClick.AddListener(OnJoinButtonClicked);
    }
    
    private void OnStartButtonClicked()
    {
        SessionManager.Instance.OpenSession();
    }

    private void OnJoinButtonClicked()
    {
        var joinCode = _joinCodeInputField.text;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Join code is empty.");
            return;
        }
        
        SessionManager.Instance.JoinSession(joinCode);
    }
}
