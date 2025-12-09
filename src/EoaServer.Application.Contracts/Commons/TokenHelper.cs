namespace EoaServer.Commons;

public enum TokenType
{
    Token,
    NFTCollection,
    NFTItem
}

public class TokenHelper
{
    public static TokenType GetTokenType(string symbol)
    {
        if (!symbol.Contains('-')) return TokenType.Token;
        var arr = symbol.Split("-");
        long.TryParse(arr[1], out long itemId);
        return itemId > 0 ? TokenType.NFTItem : TokenType.NFTCollection;
    }

    public static string GetNFTCollectionSymbol(string nftItemSymbol)
    {
        if (nftItemSymbol.Contains('-'))
        {
            return nftItemSymbol.Substring(0, nftItemSymbol.LastIndexOf("-")) + "-0";
        }
        
        return "";
    }

    public static long GetNFTItemId(string nftItemSymbol)
    {
        if (!nftItemSymbol.Contains('-'))
        {
            return 0;
        }

        long.TryParse(nftItemSymbol.Substring(nftItemSymbol.LastIndexOf("-") + 1), out long tokenId);
        return tokenId;
    }
}