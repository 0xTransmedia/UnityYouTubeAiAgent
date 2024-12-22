using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using Inworld;
using Inworld.Packet;

public enum YoutubeStatus
{
    Idle, Runnig
}

public class YouTubeLiveChatHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _autoStart;
    [SerializeField] private float _commentsRefreshIntervalSec = 1.0f;
    [SerializeField] private SolanaTokenProcessor _tokenProcessor;

    [Header("Inwolrd Triggers")]
    [SerializeField] private string _introTrigger;
    [SerializeField] private string _finalTrigger;
    [SerializeField] private float _timeToFinalSec;

    [Header("Comments")]
    [SerializeField] private List<YoutubeComment> _allComments = new();
    [SerializeField] private List<YoutubeComment> _priorityComments =  new();

    public YoutubeStatus Status { get; private set; }

    public event Action StartingEvent;
    public event Action RunningEvent;
    public event Action StoppedEvent;

    private readonly string _apiUrl = "https://www.googleapis.com/youtube/v3/liveChat/messages";
    private readonly string _tokenUrl = "https://oauth2.googleapis.com/token";
    private readonly string _broadcastUrl = "https://www.googleapis.com/youtube/v3/liveBroadcasts?part=snippet&broadcastStatus=active";

    private string _refreshToken;
    private string _clientId;
    private string _clientSecret;
    private string _accessToken;
    private string _channelId;
    private string _liveChatId;
    private string _channelName;
    private int _attempts;

    private void Start()
    {
        GetSettings();

        if (_autoStart
            && !string.IsNullOrEmpty(_refreshToken)
            && !string.IsNullOrEmpty(_clientId)
            && !string.IsNullOrEmpty(_clientSecret))
            StartCoroutine(Running());
    }

    private void OnDestroy() => InworldController.Instance.OnCharacterInteraction += OnInworldInteraction;

    private void GetSettings()
    {
        _autoStart = (PlayerPrefs.GetInt("Youtube_AutoStart") == 1);
        _refreshToken = PlayerPrefs.GetString("Youtube_RefreshToken");
        _clientId = PlayerPrefs.GetString("Youtube_ClientId");
        _clientSecret = PlayerPrefs.GetString("Youtube_ClientSecret");
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("Youtube_RefreshToken", _autoStart ? 1 : 0);
        PlayerPrefs.SetString("Youtube_RefreshToken", _refreshToken);
        PlayerPrefs.SetString("Youtube_ClientId", _clientId);
        PlayerPrefs.SetString("Youtube_ClientSecret", _clientSecret);
    }

    public void Run(string refreshToken, string clientId, string clientSecret, bool autoStart)
    {
        _refreshToken = refreshToken;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _autoStart = autoStart;
        SaveSettings();

        StartCoroutine(Running());
    }

    public void Stop()
    {
        Status = YoutubeStatus.Idle;
    }
    
    private IEnumerator Running()
    {
        if (string.IsNullOrEmpty(_refreshToken)
            && string.IsNullOrEmpty(_clientId)
            && string.IsNullOrEmpty(_clientSecret))
            yield break;

        StartingEvent?.Invoke();

        while (string.IsNullOrEmpty(_accessToken))
            yield return GetAccessToken();

        while (string.IsNullOrEmpty(_liveChatId))
            yield return GetLiveChatId();

        while (string.IsNullOrEmpty(_channelName))
            yield return GetChannelName();

        while (InworldController.Instance == null)
            yield return new WaitForFixedUpdate();

        InworldController.Instance.OnCharacterInteraction += OnInworldInteraction;

        Status = YoutubeStatus.Runnig;
        RunningEvent?.Invoke();
        GlobalEvents.SendYoutubeLavestreamStartEvent();
        _tokenProcessor.ProcessSucessEvent += OnTokenProcessSucess;


        while (InworldController.Client.Status != InworldConnectionStatus.Connected)
            yield return new WaitForFixedUpdate();

        InworldController.CurrentCharacter.SendTrigger(_introTrigger);
        StartCoroutine(ActivateTimerToFinal());
        
        while (Status == YoutubeStatus.Runnig)
        {
            yield return new WaitForSeconds(_commentsRefreshIntervalSec);
            yield return GetMessagesFromLiveChat();
            
            if(!InworldController.CurrentCharacter.IsSpeaking)
                ReplyOnPreviousMessage();
        }

        StoppedEvent?.Invoke();
        GlobalEvents.SendYoutubeLavestreamEndEvent();
        _tokenProcessor.ProcessSucessEvent -= OnTokenProcessSucess;
    }

    private IEnumerator ActivateTimerToFinal()
    {
        var character = InworldController.CurrentCharacter;
        yield return new WaitForSeconds(_timeToFinalSec);

        while (character.IsSpeaking)
            yield return new WaitForFixedUpdate();

        Status = YoutubeStatus.Idle;
        InworldController.CurrentCharacter.SendTrigger(_finalTrigger);
        _tokenProcessor.ProcessTokens();
    }

    private IEnumerator GetAccessToken()
    {
        WWWForm form = new WWWForm();
        form.AddField("client_id", _clientId);
        form.AddField("client_secret", _clientSecret);
        form.AddField("refresh_token", _refreshToken);
        form.AddField("grant_type", "refresh_token");

        UnityWebRequest tokenRequest = UnityWebRequest.Post(_tokenUrl, form);
        yield return tokenRequest.SendWebRequest();

        if (tokenRequest.result == UnityWebRequest.Result.Success)
        {
            TokenResponse response = JsonUtility.FromJson<TokenResponse>(tokenRequest.downloadHandler.text);
            _accessToken = response.access_token;
            Debug.Log("[Youtube] Access token refreshed.");
        }
        else
        {
            OnApiFailed();
            Debug.LogError("[Youtube] Failed to refresh Access Token " + tokenRequest.error);
        }
    }

    private IEnumerator GetLiveChatId()
    {
        UnityWebRequest broadcastRequest = UnityWebRequest.Get(_broadcastUrl);
        broadcastRequest.SetRequestHeader("Authorization", "Bearer " + _accessToken);

        yield return broadcastRequest.SendWebRequest();

        if (broadcastRequest.result == UnityWebRequest.Result.Success)
        {
            LiveBroadcastResponse response = JsonUtility.FromJson<LiveBroadcastResponse>(broadcastRequest.downloadHandler.text);
            if (response.items.Length > 0)
            {
                _liveChatId = response.items[0].snippet.liveChatId;
                _channelId = response.items[0].snippet.channelId;

                Debug.Log("[Youtube] LiveChatId: " + _liveChatId);
            }
            else
            {
                OnApiFailed();
                Debug.LogError("[Youtube] No active livestreams found.");
            }
        }
        else if (broadcastRequest.responseCode == 401)
        {
            Debug.LogWarning("[Youtube] Access token has expired. Refresh the token...");
            yield return GetAccessToken();
            yield return GetLiveChatId();
        }
        else 
        {
            OnApiFailed();
            Debug.LogError("[Youtube] Failed to get the LiveChatId: " + broadcastRequest.error);
        }
    }

    private IEnumerator GetChannelName()
    {
        string channelUrl = $"https://www.googleapis.com/youtube/v3/channels?part=snippet&id={_channelId}";

        UnityWebRequest channelRequest = UnityWebRequest.Get(channelUrl);
        channelRequest.SetRequestHeader("Authorization", "Bearer " + _accessToken);

        yield return channelRequest.SendWebRequest();

        if (channelRequest.result == UnityWebRequest.Result.Success)
        {
            ChannelResponse response = JsonUtility.FromJson<ChannelResponse>(channelRequest.downloadHandler.text);
            if (response.items.Length > 0)
            {
                _channelName = response.items[0].snippet.title;
                Debug.Log("[YouTube] Channel name: " + _channelName);
            }
            else
            {
                OnApiFailed();
                Debug.LogError("[YouTube] Failed to get the ChannelInfo.");
            }
        }
        else 
        {
            OnApiFailed();
            Debug.LogError("[YouTube] Failed to get the ChannelName: " + channelRequest.error);
        }
    }

    public IEnumerator GetMessagesFromLiveChat()
    {
        string chatMessagesUrl = "https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId=" + _liveChatId + "&part=snippet,authorDetails";

        UnityWebRequest request = UnityWebRequest.Get(chatMessagesUrl);
        request.SetRequestHeader("Authorization", "Bearer " + _accessToken);

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            JObject jsonResponse = JObject.Parse(request.downloadHandler.text);
            JArray items = (JArray)jsonResponse["items"];

            if (items.Count == _allComments.Count)
                yield break;

            foreach (var item in items)
            {
                string time = item["snippet"]?["publishedAt"]?.ToString();
                string authorName = item["authorDetails"]?["displayName"]?.ToString();
                string message = item["snippet"]?["displayMessage"]?.ToString();

                if (authorName == _channelName)
                    continue;

                if (_allComments.Find(comment => comment.Time == DateTime.Parse(time)) == null)
                {
                    bool isPriority = false;
                    SolanaToken token = _tokenProcessor.ExtractTokenFromMessage(message);
                    if (token != null)
                        isPriority = _tokenProcessor.TryAddTokenToProcess(token);

                    var newComment = new YoutubeComment(time, authorName, message, isPriority);
                    _allComments.Add(newComment);
                    
                    if (isPriority)
                        _priorityComments.Add(newComment);

                    Debug.Log($"[Youtube] New comment! {authorName}: {message}");
                }
            }
        }
        else 
        {
            OnApiFailed();
            Debug.LogError("[Youtube] Failed to get Messages from LiveChat: " + request.error);
        }
    }

    public IEnumerator SendMessageToLiveChat(string messageText)
    {
        ChatMessageBody messageBody = new ChatMessageBody(_liveChatId, messageText);
        string jsonMessage = JsonUtility.ToJson(messageBody);

        UnityWebRequest messageRequest = new UnityWebRequest(_apiUrl + "?part=snippet", "POST");
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonMessage);
        messageRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
        messageRequest.downloadHandler = new DownloadHandlerBuffer();
        messageRequest.SetRequestHeader("Authorization", "Bearer " + _accessToken);
        messageRequest.SetRequestHeader("Content-Type", "application/json");

        yield return messageRequest.SendWebRequest();
        if (messageRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[YouTube] Message successfully sent to chat.");
        }
        else if (messageRequest.responseCode == 401)
        {
            Debug.Log("[YouTube] Access token has expired. Refresh the token...");
            yield return GetAccessToken();
            yield return SendMessageToLiveChat(messageText);
        }
        else
        {
            OnApiFailed();
            Debug.LogError("[YouTube] Failed to sen mesaget to the LiveChat " + messageRequest.error);
        }
    }

    private void SendMessageToCharacter(YoutubeComment comment)
    {
        var currentChar = InworldController.CurrentCharacter;

        if (InworldController.CharacterHandler.IsAnyCharacterSpeaking) return;
        if (currentChar == null || currentChar.IsSpeaking) return;
        
        currentChar.SendText($"A user {comment.AuthorName} told you: {comment.Message}. Reply using his name.");
        comment.MarkAsReplied();
    }

    private void OnInworldInteraction(InworldPacket incomingPacket)
    {
        var textPacket = incomingPacket as TextPacket;
        if (textPacket == null || textPacket.routing.source.type.ToUpper() != "AGENT") return;

        StartCoroutine(SendMessageToLiveChat(textPacket.text.text));
    }

    private void OnTokenProcessSucess(List<SolanaToken> tokens)
    {
        if (tokens == null || tokens.Count == 0) return;

        string message = "ISay that you have decided to buy the following tokens: ";
        foreach (var token in tokens)
            message += token.Ticker + ", ";

        StartCoroutine(SayWhatBuy(message));
    }

    private IEnumerator SayWhatBuy(string message)
    {
        var currentChar = InworldController.CurrentCharacter;
        while (currentChar == null || currentChar.IsSpeaking)
            yield return new WaitForFixedUpdate();

        currentChar.SendText(message);
    }

    private void OnApiFailed()
    {
        _attempts++;
        if (_attempts >= 5)
        {
            _attempts = 0;
            StopAllCoroutines();
            StoppedEvent?.Invoke();
            GlobalEvents.SendYoutubeLavestreamEndEvent();
        }
    }

    private void ReplyOnPreviousMessage()
    {
        var commentsGroup = GetCommentGroupToReply(_priorityComments);
        if (commentsGroup == null || commentsGroup.Count == 0)
        {
            commentsGroup = GetCommentGroupToReply(_allComments);
            if (commentsGroup == null || commentsGroup.Count == 0) return;
        }
        
        YoutubeComment commentToReply;

        if (commentsGroup.Count > 1)
        {
            var message = new StringBuilder();
            foreach (var comment in commentsGroup)
                message.Insert(0, $"{comment.Message} ");

            commentToReply = new(commentsGroup[0].Time.ToString(), commentsGroup[0].AuthorName, message.ToString(), false);
        }
        else commentToReply = commentsGroup[0];

        foreach (var comment in commentsGroup)
            comment.MarkAsReplied();

        Debug.Log($"[Youtube] Replied to: {commentToReply.AuthorName} - {commentToReply.Message}");
        SendMessageToCharacter(commentToReply);
    }

    private List<YoutubeComment> GetCommentGroupToReply(List<YoutubeComment> _comments)
    {
        var commentsToReply = new List<YoutubeComment>();

        for (int i = _comments.Count - 1; i >= 0 ; i--)
        {
            if (!_comments[i].IsReplied)
            {
                commentsToReply.Add(_comments[i]);
                int prevIndex = i - 1;
                while (prevIndex >= 0 && !_comments[prevIndex].IsReplied)
                {
                    if (_comments[prevIndex].AuthorName == _comments[i].AuthorName)
                        commentsToReply.Add(_comments[prevIndex]);
                    else return commentsToReply;

                    prevIndex--;
                }

                return commentsToReply;
            }
        }

        return commentsToReply;
    }

    [System.Serializable]
    public class YoutubeComment
    {
        [SerializeField] private DateTime _time;
        [SerializeField] private string _authorName;
        [SerializeField] private string _message;
        [SerializeField] private string _tokenAdress;
        [SerializeField] private string _tokenTicker;
        [SerializeField] private bool _isReplied;
        [SerializeField] private bool _isPriority;
        [SerializeField] private bool _isGrouped;

        public DateTime Time => _time;
        public string AuthorName => _authorName;
        public string Message => _message;
        public bool IsReplied => _isReplied;
        public bool IsPriority => _isPriority;
        public bool IsGrouped => _isGrouped;

        public YoutubeComment(string time, string authorName, string message, bool isPririty)
        {
            _time = DateTime.Parse(time);
            _authorName = authorName;
            _message = message;
            _isPriority = isPririty;
        }

        public void MarkAsReplied()
        {
            _isReplied = true;
        }
        public void MarkAsGrouped()
        {
            _isGrouped = true;
        }
    }

    [System.Serializable]
    private class TokenResponse
    {
        public string access_token;
        public int expires_in;
        public string scope;
        public string token_type;
    }

    [System.Serializable]
    private class ChannelResponse
    {
        public ChannelItem[] items;

        [System.Serializable]
        public class ChannelItem
        {
            public Snippet snippet;

            [System.Serializable]
            public class Snippet
            {
                public string title; // Имя канала
            }
        }
    }

    [System.Serializable]
    private class ChatMessageBody
    {
        public Snippet snippet;

        public ChatMessageBody(string liveChatId, string messageText)
        {
            snippet = new Snippet(liveChatId, messageText);
        }

        [System.Serializable]
        public class Snippet
        {
            public string liveChatId;
            public string type = "textMessageEvent";
            public TextMessageDetails textMessageDetails;

            public Snippet(string liveChatId, string messageText)
            {
                this.liveChatId = liveChatId;
                this.textMessageDetails = new TextMessageDetails(messageText);
            }
        }

        [System.Serializable]
        public class TextMessageDetails
        {
            public string messageText;

            public TextMessageDetails(string messageText)
            {
                this.messageText = messageText;
            }
        }
    }

    [System.Serializable]
    private class LiveBroadcastResponse
    {
        public BroadcastItem[] items;

        [System.Serializable]
        public class BroadcastItem
        {
            public Snippet snippet;
        }

        [System.Serializable]
        public class Snippet
        {
            public string liveChatId;
            public string channelId;
        }
    }
}
