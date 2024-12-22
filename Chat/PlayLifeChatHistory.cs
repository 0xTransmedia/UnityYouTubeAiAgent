using System;
using System.Collections.Generic;

[Serializable]
public class PlayLifeChatHistory
{
    public List<PlayLifeChatHistorySession> Sessions;
}

[Serializable]
public class PlayLifeChatHistorySession 
{
    public string SceneFullName;
    public string SessionToken;
    public string CharacterName;
    public DateTime LastChatTime;
    public List<PlayLifeChatHistoryPhrase> Phrases;
}

[Serializable]
public class PlayLifeChatHistoryPhrase
{
    public string ID;
    public string Type;
    public string Source;
    public string Name;
    public string Text;
}