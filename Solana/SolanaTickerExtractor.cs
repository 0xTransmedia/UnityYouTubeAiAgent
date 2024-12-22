using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SolanaTickerExtractor
{
    public List<string> ExtractSolanaTickers(string message)
    {
        string pattern = @"\$(\w+)";
        var matches = Regex.Matches(message, pattern);

        List<string> tickers = new List<string>();
        foreach (Match match in matches)
        {
            tickers.Add(match.Groups[1].Value); 
        }

        return tickers;
    }
}
