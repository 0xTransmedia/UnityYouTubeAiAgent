using UnityEngine;
using Inworld.Packet;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Inworld.UI;
using Inworld;
using System.Linq;

public class PlayLifeChatPanel : Inworld.Sample.ChatPanel
{
    [SerializeField] private RectTransform _BubbleContainer;
    [SerializeField] private ChatBubbleSelfie _bubbleSelfie;
    [SerializeField] private WaitingForAnswer _waitForAnswer;
    [SerializeField] private PhotoViewer _photoViewer;

    public event Action<PlayLifeChatHistoryPhrase> PhraseRecievedEvent;
    public event Action PlayerInteractionEvent;

    private DeepLTranslator _translator = new DeepLTranslator();
    private string _targetLang;

    private CharacterSelfiesSO _currentSelfiePack;

    public void SendPlayerText(string text)
    {
        var newBubble = Instantiate(m_BubbleRight, _BubbleContainer);
        newBubble.SetBubble(InworldAI.User.Name, InworldAI.DefaultThumbnail, text);
    }

    public void SetSelfiePack(CharacterSelfiesSO selfiePack)
    {
        _currentSelfiePack = selfiePack;
    }

    public void SendCharacterSelfie(Selfie selfie)
    {
        CreateSelfieBubble(selfie);
        SendPhraseEvent(selfie);
    }

    private void CreateSelfieBubble(Selfie selfie)
    {
        var newSelfie = Instantiate(_bubbleSelfie, _BubbleContainer);
        newSelfie.Init(selfie);
        newSelfie.ShowPhotoEvent += OnShowPhoto;
    }

    protected override async void HandleText(TextPacket textPacket)
    {
        UpdateTargetLang();

        if (!m_ChatOptions.text 
            || textPacket.text == null 
            || string.IsNullOrWhiteSpace(textPacket.text.text) 
            || !IsUIReady)
            return;

        if (_targetLang != "en")
            textPacket.text.text = await Translate(textPacket.text.text);

        SendPhraseEvent(textPacket);

        switch (textPacket.routing.source.type.ToUpper())
        {
            case "AGENT":
                _waitForAnswer.gameObject.SetActive(false);
                base.HandleText(textPacket);
                break;
            case "PLAYER":
                _waitForAnswer.transform.SetAsLastSibling();
                _waitForAnswer.gameObject.SetActive(true);
                break;
        }
    }

    protected override async void HandleAction(ActionPacket actionPacket)
    {
        UpdateTargetLang();

        if (!m_ChatOptions.narrativeAction
            || actionPacket.action == null
            || actionPacket.action.narratedAction == null 
            || string.IsNullOrWhiteSpace(actionPacket.action.narratedAction.content) 
            || !IsUIReady)
            return;

        if (_targetLang != "en")
            actionPacket.action.narratedAction.content 
                = await Translate(actionPacket.action.narratedAction.content);

        SendPhraseEvent(actionPacket);

        base.HandleAction(actionPacket);
    }

    public void LoadHistory(List<PlayLifeChatHistoryPhrase> phrases)
    {
        if (phrases == null) return;

        foreach (var phrase in phrases)
        {
            if (phrase.Type == "SELFIE")
            {
                Selfie selfie = _currentSelfiePack.Selfies.FirstOrDefault((selfie) => selfie.Id == phrase.ID);
                if (selfie != null)
                    CreateSelfieBubble(selfie);
            }
            else
            {
                ChatBubble chatBubble = phrase.Source == "AGENT"
                ? Instantiate(m_BubbleLeft, _BubbleContainer)
                : Instantiate(m_BubbleRight, _BubbleContainer);

                chatBubble.Text = phrase.Text;
            }
        }
    }

    public void ClearHistory()
    {
        foreach (Transform transform in _BubbleContainer)
        {
            if(transform.TryGetComponent<ChatBubble>(out ChatBubble bubble))
                Destroy(bubble.gameObject);
            else if (transform.TryGetComponent<ChatBubbleSelfie>(out ChatBubbleSelfie selfie))
                Destroy(selfie.gameObject);
        }
    }

    private void SendPhraseEvent(InworldPacket packet)
    {  
        PlayLifeChatHistoryPhrase phrase = new();
        phrase.ID = packet.packetId.packetId;
        phrase.Type = packet.type;
        phrase.Source = packet.routing.source.type;
        phrase.Name = packet.routing.source.name;

        var textPacket = packet as TextPacket;
        var actionPacket = packet as ActionPacket;
        if (textPacket != null)
        {
            phrase.Text = textPacket.text.text;
        }
        else if (actionPacket != null)
        {
            phrase.Text = actionPacket.action.narratedAction.content;
        }

        PhraseRecievedEvent?.Invoke(phrase);
    }

    private void SendPhraseEvent(Selfie selfie)
    {
        PlayLifeChatHistoryPhrase phrase = new();
        phrase.ID = selfie.Id;
        phrase.Type = "SELFIE";
        phrase.Source = "AGENT";
        phrase.Text = "Photo";

        PhraseRecievedEvent?.Invoke(phrase);
    }

    private void UpdateTargetLang()
    {
        _targetLang = GameManager.Instance != null ? GameManager.Instance.Language : "en";
    }

    private async Task<string> Translate(string text)
    {
        string translation = await _translator.Translate(text, _targetLang);
        if (!string.IsNullOrEmpty(translation))
            return translation;
        else return text;
    }

    private void OnShowPhoto(Selfie selfie)
    {
        _photoViewer.ShowPhoto(selfie);
    }
}