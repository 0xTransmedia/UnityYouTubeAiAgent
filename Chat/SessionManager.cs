using Inworld;
using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    [SerializeField] private PlayLifeChatPanel _chat;
    
    [Header("Triggers")]
    [SerializeField] private string _helloTrigger = "first_meetinglover";
    [SerializeField] private string _reEntryTrigger = "lover";
    
    [Header("CloseDialog")]
    [SerializeField] private Button _resetSessionButton;
    [SerializeField] private Button _quitButton;
    
    [Header("Reset")]
    [SerializeField] private ConfirmationPopup _confirmationPopup;
    [SerializeField] private string _title;
    [SerializeField] private string _message;
    [SerializeField] private string _yesText;
    [SerializeField] private string _noText;

    private bool _isSave = true;
    private bool _isChating;
    private InworldClient _client;
    private PlayLifeChatHistory _history;
    private PlayLifeChatHistorySession _currentSessionHistory;

    private void OnEnable()
    {
        _chat.PhraseRecievedEvent += OnPhraseRecieved;
        _resetSessionButton.onClick.AddListener(OnResetButtonClick);
        _confirmationPopup.ConfirmEvent += OnResetConfirmation;

        GlobalEvents.SwitchToChatEvent += OnSwitchToChat;
        GlobalEvents.SwitchToTapEvent += OnSwitchToTap;
    }

    private void OnDestroy()
    { 
        if(!_isSave)
            SaveSession();

        _chat.PhraseRecievedEvent -= OnPhraseRecieved;
        _resetSessionButton.onClick.RemoveListener(OnResetButtonClick);
        _confirmationPopup.ConfirmEvent -= OnResetConfirmation;

        GlobalEvents.SwitchToChatEvent -= OnSwitchToChat;
        GlobalEvents.SwitchToTapEvent -= OnSwitchToTap;
    }

    public void StartSession()
    {
        StartCoroutine(StartSessionAsync());
    }

    private void CreateNewSession()
    {
        _currentSessionHistory = new();
        _currentSessionHistory.Phrases = new();
        _currentSessionHistory.SceneFullName = InworldController.Instance.CurrentScene;
        _currentSessionHistory.CharacterName = InworldController.CurrentCharacter.Name;
        _history.Sessions.Add(_currentSessionHistory);
    }

    public void ResetSession()
    {
        InworldController.CurrentCharacter.CancelResponse();
        _history.Sessions.Remove(_currentSessionHistory);
        _chat.ClearHistory();
        CreateNewSession();
        InworldController.CurrentCharacter.SendTrigger(_helloTrigger);
    }

    public void EndSession()
    {
        if (!_isSave)
            SaveSession();
        _chat.ClearHistory();
    }

    public void ContinueSession()
    {
        string history = _currentSessionHistory.SessionToken;
        _chat.LoadHistory(_currentSessionHistory.Phrases);
        Debug.Log($"Load History: {history}");
        _client.SessionHistory = history;
    }

    private IEnumerator StartSessionAsync()
    {
        _history = GameManager.Instance.History;

        while (InworldController.Client == null 
            || InworldController.Client.Status != InworldConnectionStatus.Connected 
            || InworldController.CurrentCharacter == null)
            yield return new WaitForFixedUpdate();
        
        _client = InworldController.Client;

        _currentSessionHistory = GetCurrentSessionHistory();

        if (_currentSessionHistory != null)
            ContinueSession();
        else
            CreateNewSession();
        
        SayHello();
    }

    private void SayHello()
    {
        if (!_isChating) return;

        if(_currentSessionHistory != null && _currentSessionHistory.Phrases.Count > 0)
            InworldController.CurrentCharacter.SendTrigger(_reEntryTrigger);
        else InworldController.CurrentCharacter.SendTrigger(_helloTrigger);
    }

    public void SaveSession()
    {
        if (_currentSessionHistory.Phrases.Count == 0)
        {
            _history.Sessions.Remove(_currentSessionHistory);
            return;
        }

        string sessionToken = _client.SessionHistory;
        Debug.Log($"History saved: {sessionToken}");

        _currentSessionHistory.SessionToken = sessionToken;
        _currentSessionHistory.LastChatTime = DateTime.Now;

        GameManager.Instance.SaveHistory();
        _isSave = true;
    }

    private void OnSwitchToChat()
    {
        _isChating = true;
    }

    private void OnSwitchToTap()
    {
        _isChating = false;
    }

    private void OnResetButtonClick() => _confirmationPopup.AskToConfirm(_title, _message, _yesText, _noText);

    private void OnResetConfirmation(bool comfirmed)
    {
        if (comfirmed)
            ResetSession();
    }

    private void OnPhraseRecieved(PlayLifeChatHistoryPhrase phrase)
    {
        _isSave = false;
        _currentSessionHistory.Phrases.Add(phrase);
        SaveSession();
    }

    private PlayLifeChatHistorySession GetCurrentSessionHistory()
    {
        return _history.Sessions.FirstOrDefault(
            entry => entry.SceneFullName == InworldController.Instance.CurrentScene);
    }
}
