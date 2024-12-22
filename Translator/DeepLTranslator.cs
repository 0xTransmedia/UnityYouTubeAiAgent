using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class TranslationList
{
    public Translation[] translations;
}

[System.Serializable]
public class Translation
{
    public string detected_source_language;
    public string text;
}

public class DeepLTranslator
{
    private RequestUtil _request = new RequestUtil();

    //private string _deepLProxyURL = "https://33w9d5kf3b.execute-api.us-east-1.amazonaws.com/default/DeepLProxy";
    private string _deepLProxyURL = "https://translator.transmedia.games/DeepLProxy";
    
    public async Task<string> Translate(string text, string targetLang)
    {
        string url = _deepLProxyURL + "?target_lang=" + targetLang + "&text=" + text;
        Dictionary<string, string> headers = new Dictionary<string, string>{ { "Authorization", 
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3MjI1MDI4NzksImF1ZCI6InRyYW5zbGF0b3IifQ.IljCfki8oK9DN_Tsyd8gZrQjsKT9yRlsujdVf0-GZ5c" } };

        try
        {
            object response = await _request.AsyncGET(url, headers, "text");
            TranslationList translationList = JsonUtility.FromJson<TranslationList>(response.ToString());
            return translationList.translations[0].text;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            return null;
        }
    }
}