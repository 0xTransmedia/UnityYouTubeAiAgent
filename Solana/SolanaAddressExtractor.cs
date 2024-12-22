using System;
using System.Text.RegularExpressions;
using System.Numerics;

public class SolanaAddressExtractor
{
    private static readonly string _solanaAddressPattern 
        = @"\b[1-9A-HJ-NP-Za-km-z]{32,44}\b";
    private static readonly char[] _base58Alphabet 
        = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".ToCharArray();

    public string ExtractSolanaAddress(string message)
    {
        if (string.IsNullOrEmpty(message)) return null;

        // »щем совпадение с Solana-адресом в сообщении
        Match match = Regex.Match(message, _solanaAddressPattern);

        if (match.Success)
        {
            string possibleAddress = match.Value;

            // ѕровер€ем, действительно ли это валидный Solana-адрес
            if (IsValidSolanaAddress(possibleAddress))
            {
                return possibleAddress;
            }
        }

        return null; // јдрес не найден или не валиден
    }

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

    private static byte[] Base58Decode(string input)
    {
        BigInteger intData = BigInteger.Zero;
        foreach (char c in input)
        {
            int digit = Array.IndexOf(_base58Alphabet, c);
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
