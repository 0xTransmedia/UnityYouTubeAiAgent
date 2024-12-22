using Inworld.Sample;
using System;
using UnityEngine;
using TMPro;
using Inworld;
using UnityEngine.UI;
using Playlife;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

public class PlayLifePlayer : PlayerController3D
{
    private const string TRIGGER_CONTINUE = "continue";

    public FantasySO _fantasy;
    [SerializeField] private PlayLifeChatPanel _chat;
    [SerializeField] private TMP_Text _gemsLabel;
    [SerializeField] private TMP_Text _pointsLabel;
    [SerializeField] private TMP_Text _interactionPriceLabel;
    [SerializeField] private Button _continueButton;
    [SerializeField] private bool _enableContinueButton;
    [SerializeField] private PlaylifeBubbleTrigger[] _triggerButtons;

    [SerializeField] private AudioSource _bgMusic;

    public int Gems 
    { 
        get => _gems;
        set
        {
            _gems = value;
            _gemsLabel.text = $"{_gems.ToString()}";
            PlayerBalanceChange?.Invoke(_gems);
        }
    }
    public int Interactions { get; private set; }
    public int InteractionPrice 
    { 
        get => _interactionPrice;
        set 
        {
            _interactionPrice = value;
            if (_interactionPrice > 0)
                _interactionPriceLabel.text = value.ToString();
            else _interactionPriceLabel.gameObject.SetActive(false);
        }  
    }
    public string ActiveCharacterName { get; private set; }

    public event Action<int> PlayerBalanceChange;
    public event Action<string> TriggerSendedEvent;
    public event Action<string> SendMessageEvent;

    private CloudService _cloudService;
    private PlayLifeRecordButton _recordButton;
    private string _characterId;
    private int _gems;
    private int _interactionPrice;
    private string _triggerToSend;
    private Action _triggerCallback;
    private List<Selfie> _selfies;

    protected override void OnEnable()
    {
        base.OnEnable();

        m_SendButton.onClick.AddListener(OnSendButtonClick);
        _continueButton.onClick.AddListener(OnContinueButtonClick);
        _recordButton = m_RecordButton.GetComponent<PlayLifeRecordButton>();
        _recordButton.PointerDownEvent += OnRecordButtonDown;
        _recordButton.PointerUpEvent += OnRecordButtonUp;
        _cloudService.BalanceUpdateEvent += OnBalanceUpdate;

        InworldController.Client.OnStatusChanged += OnInworldStatusChange;

        foreach (var trigger in _triggerButtons)
            trigger.TriggerButtonClickEvent += OnTriggerButtonClick;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        //AnalyticsHandleService.Instance.SendInteractions(ActiveCharacterName, Interactions);

        m_SendButton.onClick.RemoveListener(OnSendButtonClick);
        _continueButton.onClick.AddListener(OnContinueButtonClick);
        _cloudService.BalanceUpdateEvent -= OnBalanceUpdate;

        if (InworldController.Client != null)
            InworldController.Client.OnStatusChanged -= OnInworldStatusChange;
        
        if(InworldController.CurrentCharacter!= null)
        InworldController.CurrentCharacter.onEndSpeaking.RemoveListener(OnCharacterEndSpeaking);

        foreach (var trigger in _triggerButtons)
            trigger.TriggerButtonClickEvent -= OnTriggerButtonClick;
    }

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.LeftControl))
            OnCharacterTriggerReceived("send_photo_paid");

        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            OnCharacterTriggerReceived("send_photo_free");
        }
    }

    public void StartSession(string characterId, CharacterSelfiesSO selfiePack)
    { 
        _characterId = characterId;

        if(selfiePack != null)
            _selfies = selfiePack.Selfies.ToList();
        _chat.SetSelfiePack(selfiePack);

        _cloudService = CloudService.Instance;

        GetCloudData();
    }

    public void StopSession()
    {
        _selfies = null;
    }

    public void OnSwitchToChat()
    {
        _bgMusic.Play();
    }

    public void OnSwitchToTap()
    {
        _bgMusic.Stop();
    }

    private void GetCloudData()
    {
        Gems = _cloudService.PlayerBalance;
        InteractionPrice = 0;//_cloudService.InteractionPrice;
        UpdatePoints();
    }

    private void OnBalanceUpdate(int amount)
    {
        Gems = amount;
        UpdatePoints();
    }

    private async Task UpdatePoints()
    {
        var result = await CloudService.Instance.GetPlayerScore();
        double points = result != null ? result.Score : 0;
        _pointsLabel.text = points.ToString();
    }

    public void OnSendButtonClick()
    {
        if (string.IsNullOrEmpty(m_InputField.text)) return;

        SendMessageEvent?.Invoke(m_InputField.text);
        _chat.SendPlayerText(m_InputField.text);
        InteractionHandle(SendText, InteractionPrice);
        AnalyticsHandleService.Instance.SendMessages(ActiveCharacterName);
    }

    private void OnContinueButtonClick()
    {
        m_InputField.text = "Please, continue...";
        _continueButton.transform.parent.gameObject.SetActive(false);
        InteractionHandle(SendText, InteractionPrice);
    }

    private void OnRecordButtonUp()
    {
        if (!InteractionHandle(InworldController.Instance.PushAudio, InteractionPrice))
            InworldController.Instance.StopAudio();
    }
    
    private void OnRecordButtonDown()
    {
        InworldController.Instance.StartAudio();
    }

    private void OnTriggerButtonClick(string trigger, int price, Action callback)
    {
        _triggerToSend = trigger;
        _triggerCallback = callback;
        InteractionHandle(SendTrigger, price);
    }

    private void OnInworldStatusChange(InworldConnectionStatus status)
    {
        if (status == InworldConnectionStatus.Connected)
        {
            ActiveCharacterName = InworldController.CurrentCharacter.Name;
            //_titleLabel.text = ActiveCharacterName;

            AnalyticsHandleService.Instance.SendStartDialog(ActiveCharacterName);

            InworldController.CurrentCharacter.onBeginSpeaking.AddListener(OnCharacterBeginSpeaking);
            InworldController.CurrentCharacter.onEndSpeaking.AddListener(OnCharacterEndSpeaking);
            InworldController.CurrentCharacter.onGoalCompleted.AddListener(OnCharacterTriggerReceived);
        }
    }

    private void OnCharacterBeginSpeaking()
    {
        if (_enableContinueButton)
            _continueButton.transform.parent.gameObject.SetActive(false);
    }

    private void OnCharacterEndSpeaking()
    {
        if (_enableContinueButton)
            _continueButton.transform.SetAsLastSibling();
    }

    private void OnCharacterTriggerReceived(string trigger)
    {
        if (trigger.Contains("send_photo"))
            OnCharacterSendSelfie(trigger);
    }

    private void OnCharacterSendSelfie(string trigger)
    {
        if (_selfies == null || _selfies.Count == 0) return;

        if (trigger == "send_photo_free")
            SendFreeSelfie();
        else if (trigger == "send_photo_paid")
            SendPaidSelfie();
    }

    private bool CanInteract(int price) => (Gems - price) >= 0;

    private bool InteractionHandle(Action interactionMethod, int price)
    {
        if (price == 0)
        {
            Interactions++;
            interactionMethod();
            return true;
        }

        if (CanInteract(price))
        {
            Gems -= price;
            Interactions++;
            interactionMethod();
            _cloudService.DecrementBalanceForInteraction(price, _characterId);
            return true;
        }

        GlobalEvents.SendNotEnoughGemsEvent();
        Debug.LogError("Not enough Gems!");
        return false;
    }

    private void SendTrigger()
    {
        if (!string.IsNullOrEmpty(_triggerToSend))
        {
            InworldController.CurrentCharacter.SendTrigger(_triggerToSend);
            _triggerCallback.Invoke();
            TriggerSendedEvent?.Invoke(_triggerToSend);
            _triggerToSend = string.Empty;
            _triggerCallback = null;
        }
    }

    private void SendFreeSelfie()
    {
        Selfie toSend = _selfies.FirstOrDefault((selfie) 
            => selfie.Type == SelfieType.Free);
        if (toSend == null) return;

        _chat.SendCharacterSelfie(toSend);
        _selfies.Remove(toSend);
    }

    private void SendPaidSelfie()
    {
        Selfie toSend = _selfies.FirstOrDefault((selfie) 
            => selfie.Type == SelfieType.Standart || selfie.Type == SelfieType.Premium);
        if (toSend == null) return;

        _chat.SendCharacterSelfie(toSend);
        _selfies.Remove(toSend);
    }
}
