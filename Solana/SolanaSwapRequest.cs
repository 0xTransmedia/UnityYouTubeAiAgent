using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

public class SolanaSwapRequest
{
    private const string API_URL = "https://arbitrumbebop.velvetdao.xyz/getQuote";
    
    public IEnumerator MakeMultiSwaps(List<SwapRequest> swapRequests)
    {
        if (swapRequests.Count > 0)
        {
            foreach (var swapRequest in swapRequests) 
                yield return MakeSwap(swapRequest);
        }
    }

    public IEnumerator MakeSwap(SwapRequest swapRequest)
    {
        string jsonData = JsonConvert.SerializeObject(swapRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Swap] Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("[Swap] Error: " + request.error);
                Debug.LogError("[Swap] Response: " + request.downloadHandler.text);
            }
        }
    }
}

[System.Serializable]
public class SwapRequest
{
    public string tokenIn;
    public string tokenOut;
    public int amount;
    public int slippage;
    public string sender;
    public float priorityFee;
}

