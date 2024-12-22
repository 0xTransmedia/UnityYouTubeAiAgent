using System;
using System.Collections.Generic;
using UnityEngine;

public class SolanaTokenProcessor : MonoBehaviour
{
    private const string TOKEN_IN = "So11111111111111111111111111111111111111112";

    [Header("Process settings")]
    [SerializeField] private int _amount;
    [SerializeField] private int _slippage;
    [SerializeField] private string _sender;
    [SerializeField] private float _priorityFee;
    
    [Header("Input Solana Tokens")]
    [SerializeField] private List<SolanaToken> _coverTokens;
    [SerializeField] private List<SolanaToken> _requiredTokens;
 
    [Header("Output Solana Tokens")]
    [SerializeField] private List<SolanaToken> _tokensToProcess = new();

    public event Action<List<SolanaToken>> ProcessSucessEvent;

    private SolanaAddressExtractor _addressExtractor = new ();
    private SolanaTickerExtractor _tickerExtractor = new ();
    private SolanaSwapRequest _swapRequest = new ();

    public SolanaToken ExtractTokenFromMessage(string message)
    {
        CheckMessageHasTokenAdress(message, out string address);
        CheckMessageHasTokenTicker(message, out string ticker);

        if (!string.IsNullOrEmpty(address) || !string.IsNullOrEmpty(ticker))
            return new SolanaToken(address, ticker);

        return null;
    }

    public bool TryAddTokenToProcess(SolanaToken token)
    {
        if (!string.IsNullOrEmpty(token.Address))
        {
            if (CheckIsAddressCover(token.Address, out SolanaToken addressToProcess))
            {
                SolanaToken alreadyAdded = _tokensToProcess.Find(item => item.Address == token.Address);
                if (alreadyAdded == null)
                    _tokensToProcess.Add(addressToProcess);
                
                return true;
            }
        }

        if (!string.IsNullOrEmpty(token.Ticker))
        {
            if (CheckIsTickerCover(token.Ticker, out SolanaToken addressToProcess))
            {
                SolanaToken alreadyAdded = _tokensToProcess.Find(item => item.Ticker == token.Ticker);
                if (alreadyAdded == null)
                    _tokensToProcess.Add(addressToProcess);
                
                return true;
            }
        }

        return false;
    }

    public void ProcessTokens()
    {
        List<SwapRequest> swapRequests = new List<SwapRequest>();

        if (_requiredTokens != null && _requiredTokens.Count > 0)
            foreach (var token in _requiredTokens)
                swapRequests.Add(CreateSwapRequst(token));

        foreach (var token in _tokensToProcess)
            swapRequests.Add(CreateSwapRequst(token));

        // v2 Delete duplicates of requests
        StartCoroutine(_swapRequest.MakeMultiSwaps(swapRequests));

        ProcessSucessEvent?.Invoke(_tokensToProcess);
    }

    private SwapRequest CreateSwapRequst(SolanaToken token)
    {
        return new SwapRequest
        {
            tokenIn = TOKEN_IN,
            tokenOut = token.Address,
            amount = _amount,
            slippage = _slippage,
            sender = _sender,
            priorityFee = _priorityFee
        };
    }

    private bool CheckMessageHasTokenAdress(string message, out string address)
    {
        address = _addressExtractor.ExtractSolanaAddress(message);
        return !string.IsNullOrEmpty(address);
    }

    private bool CheckMessageHasTokenTicker(string message, out string ticker)
    {
        List<string> tickers = _tickerExtractor.ExtractSolanaTickers(message);
        if (tickers != null && tickers.Count > 0)
        {
            ticker = tickers[0];
            return true;
        }

        ticker = null;
        return false;
    }

    private bool CheckIsAddressCover(string address, out SolanaToken token)
    {
        if (string.IsNullOrEmpty(address))
        {
            token = null;
            return false;
        }

        token = _coverTokens.Find(token => token.Address == address);
        return token != null;
    }

    private bool CheckIsTickerCover(string ticker, out SolanaToken token)
    {
        if (string.IsNullOrEmpty(ticker))
        {
            token = null;
            return false;
        }

        token = _coverTokens.Find(token => token.Ticker.ToUpper() == ticker.ToUpper());
        return token != null;
    }
}

[System.Serializable]
public class SolanaToken
{
    public string Address;
    public string Ticker;
    public bool IsProcessed;

    public SolanaToken(string address = null, string ticker = null)
    {
        Address = address;
        Ticker = ticker;
    }
}
