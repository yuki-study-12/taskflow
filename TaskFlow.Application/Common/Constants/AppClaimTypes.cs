namespace TaskFlow.Application.Common.Constants;

/// <summary>
/// JWT クレームの型名定数。
/// MapInboundClaims = false の前提で、RFC 7519 のクレーム名をそのまま使用する。
/// </summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
}
