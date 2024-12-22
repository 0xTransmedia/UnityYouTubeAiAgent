using Newtonsoft.Json;
using Playlife;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubbleSelfie : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private GameObject _locker;
    [SerializeField] private TMP_Text _lockerText;
    [SerializeField] private Color _lockedColor;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _price;
    [SerializeField] private TMP_Text _priceLabel;

    public event Action<Selfie> ShowPhotoEvent;

    private Selfie _selfie;
    private string _productId;
    private bool _isLocked;

    private Transaction _currentTransaction;
    private List<string> _availableSelfies;

    public void Init(Selfie selfie)
    {
        _selfie = selfie;
        _productId = _selfie.Type == SelfieType.Standart ? "SELFIE_STANDART" : "SELFIE_PREMIUM";
        
        int price = GameShop.Instance.GetPrice(_productId, TransactionType.Multi);
        if (price > 0)
            _priceLabel.text = price.ToString();

        UpdateState();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnButtonClick);

        GlobalEvents.PurcahseSuccessEvent += OnPurchaseSuccess;
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnButtonClick);
        GlobalEvents.PurcahseSuccessEvent -= OnPurchaseSuccess;
    }

    private void UpdateState()
    {
        if (CheckIsPurchased())
            Unlock();
        else Lock();
    }

    private void Lock()
    {
        _image.sprite = _selfie.Locked;
        _image.color = _lockedColor;
        _locker.SetActive(true);
        _isLocked = true;

        if (_selfie.Type == SelfieType.Free)
        {
            _price.SetActive(false);
            _lockerText.text = "Unlock free";
        }
        else
        {
            _price.SetActive(true);
            _lockerText.text = "Unlock";
        }
    }

    private void Unlock()
    {
        _image.sprite = _selfie.Main;
        _image.color = Color.white;
        _locker.SetActive(false);
        _price.SetActive(false);
        _isLocked = false;

        AnalyticsHandleService.Instance.SendPhotoUnlock(_selfie.Id, _selfie.Type);
    }

    private void OnButtonClick()
    {
        if (_isLocked)
        {
            AnalyticsHandleService.Instance.SendPhotoClickOnLocked(_selfie.Id, _selfie.Type);
            if (_selfie.Type == SelfieType.Free)
                OnFreeClick();
            else OnPaidClick();
        }
        else
        {
            AnalyticsHandleService.Instance.SendPhotoClickOnUnlocked(_selfie.Id, _selfie.Type);
            ShowPhotoEvent?.Invoke(_selfie);
        }
    }

    private void OnFreeClick()
    {
        Unlock();

        if (_selfie.Fantasy != null)
            GlobalEvents.SendFantasyUnlockedEvent(_selfie.Fantasy);

        CloudService.Instance.SaveData(GetPayload());
    }

    private void OnPaidClick()
    {
        _currentTransaction = new(_productId, TransactionType.RealMoney, GetPayload());
        GlobalEvents.SendTryPurchaseEvent(_currentTransaction);
    }

    private void OnPurchaseSuccess(string transactionId)
    {
        if (transactionId == _currentTransaction.Id)
            Unlock();
    }

    private bool CheckIsPurchased()
    {
        var _availableSelfies = GetAvaliableSelfies();
        if (_availableSelfies == null || _availableSelfies.Count == 0)
            return false;

        return _availableSelfies.Contains(_selfie.Id);
    }

    private List<string> GetAvaliableSelfies()
    {
        object selfiesValue = CloudService.Instance.GetSavePropertyValue("Selfies");
        if (selfiesValue != null)
            return JsonConvert.DeserializeObject<List<string>>(selfiesValue.ToString());

        return null;
    }

    private Dictionary<string, object> GetPayload()
    {
        if (_availableSelfies == null)
            _availableSelfies = new();

        _availableSelfies.Add(_selfie.Id);
        return new Dictionary<string, object>()
            {
                {"Selfies", _availableSelfies }
            };
    }
}
