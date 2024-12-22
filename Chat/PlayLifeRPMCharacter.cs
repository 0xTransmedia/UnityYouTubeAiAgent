using UnityEngine;
using System;
using Inworld.Interactions;
using Inworld;
using System.Threading.Tasks;

public class PlayLifeRPMCharacter : Inworld.Sample.RPM.InworldRPMCharacter
{
    [SerializeField] private InworldAudioInteraction _audioInteraction;
    [SerializeField] private AudioSource _voiceSource;

    private DeepLTranslator _translator = new DeepLTranslator();

    protected override void Awake()
    {
        base.Awake();

        GlobalEvents.SwitchToChatEvent += OnSwitchToChat;
        GlobalEvents.SwitchToTapEvent += OnSwitchToTap;
        GlobalEvents.FantasyLaunchEvent += OnFantasyLaunch;
        GlobalEvents.ChangeSceneEvent += OnChangeScene;
    }

    private void OnDestroy()
    {
        GlobalEvents.SwitchToChatEvent -= OnSwitchToChat;
        GlobalEvents.SwitchToTapEvent -= OnSwitchToTap;
        GlobalEvents.FantasyLaunchEvent -= OnFantasyLaunch;
        GlobalEvents.ChangeSceneEvent -= OnChangeScene;
    }

    public override async void SendText(string text)
    {
        try
        {
            string translation = await _translator.Translate(
                text, 
                "en"
            );
            if (!string.IsNullOrWhiteSpace(translation))
            {
                text = translation;
            }
        }
        catch (Exception error)
        {
            Debug.LogError(error.Message);
        }
        base.SendText(text);
    }

    private void OnSwitchToChat()
    {
        _audioInteraction.IsMute = false;
    }

    private void OnSwitchToTap()
    {
        _audioInteraction.IsMute = true;
    }

    private async void OnFantasyLaunch(FantasySO fantasy)
    {
        if (fantasy == null) return;

        await ChangeScene(fantasy.InworldData);
        SendTrigger(fantasy.InworldTrigger);
    }

    private async void OnChangeScene(InworldGameData data) => await ChangeScene(data);

    private async Task ChangeScene(InworldGameData gameData)
    {
        CancelResponse();
        Debug.LogWarning("Changing scene...");
        InworldController.Instance.GameData = gameData;
        
        var client = InworldController.Client;
        InworldController.Client.Disconnect();

        while (client.Status == InworldConnectionStatus.Connected)
            await Task.Yield();

        Debug.LogWarning(client.Status);
        InworldController.Client.LoadScene(gameData.sceneFullName);

        while (client.Status != InworldConnectionStatus.LoadingSceneCompleted)
            await Task.Yield();

        Debug.LogWarning(InworldController.Instance.CurrentScene);

        while (client.Status != InworldConnectionStatus.Connected)
            await Task.Yield();
    }
}