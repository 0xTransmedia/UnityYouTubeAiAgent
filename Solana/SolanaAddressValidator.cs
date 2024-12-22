using System;
using System.Collections;
using System.Numerics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SolanaAddressValidator : MonoBehaviour
{
    private string rpcUrl = "https://api.mainnet-beta.solana.com";

    public void ValidateSolanaAddress(string address)
    {
        if (IsValidSolanaAddress(address))
        {
            Debug.Log("Address is valid. Checking on the blockchain....");
            StartCoroutine(CheckSolanaAccount(address));
        }
        else
        {
            Debug.LogError("Invalid format of Solana-address!");
        }
    }

    // Проверка формата Solana-адреса (Base58 и длина)
    private bool IsValidSolanaAddress(string address)
    {
        if (string.IsNullOrEmpty(address)) return false;
        if (address.Length != 44) return false;

        try
        {
            byte[] decoded = Base58Decode(address);
            return decoded.Length == 32;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerator CheckSolanaAccount(string address)
    {
        string jsonPayload = $@"
        {{
            ""jsonrpc"": ""2.0"",
            ""id"": 1,
            ""method"": ""getAccountInfo"",
            ""params"": [""{address}"", {{""encoding"": ""jsonParsed""}}]
        }}";

        using (UnityWebRequest request = new UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Ответ: {request.downloadHandler.text}");

                if (request.downloadHandler.text.Contains("\"executable\":true"))
                {
                    Debug.Log("[SolanaAddressValidator] This address is Solana smart-contract!");
                }
                else
                {
                    Debug.Log("[SolanaAddressValidator] This address is valid, but its's not a smart-contrat.");
                }
            }
            else
            {
                Debug.LogError($"[SolanaAddressValidator] Error: {request.error}");
            }
        }
    }

    private static readonly char[] Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".ToCharArray();

    private static byte[] Base58Decode(string input)
    {
        BigInteger intData = BigInteger.Zero;
        foreach (char c in input)
        {
            int digit = Array.IndexOf(Base58Alphabet, c);
            if (digit < 0)
            {
                throw new FormatException($"Invalid Base58 character `{c}` at position {input.IndexOf(c)}");
            }
            intData = intData * 58 + digit;
        }

        byte[] bytes = intData.ToByteArray();
        Array.Reverse(bytes);

        if (bytes.Length > 1 && bytes[0] == 0)
        {
            Array.Resize(ref bytes, bytes.Length - 1);
        }

        int leadingZeros = 0;
        foreach (char c in input)
        {
            if (c == '1') leadingZeros++;
            else break;
        }

        byte[] result = new byte[bytes.Length + leadingZeros];
        Array.Copy(bytes, 0, result, leadingZeros, bytes.Length);

        return result;
    }
}
