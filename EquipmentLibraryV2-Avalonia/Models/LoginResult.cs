namespace EquipmentLibraryV2_Avalonia.Models;

public enum LoginStatus
{
    Ok,
    Invalid,
    Locked,
    Throttled,
    Error
}

public sealed record LoginResult(LoginStatus Status, int? RetryAfterSeconds = null)
{
    public bool IsSuccess => Status == LoginStatus.Ok;

    public static LoginResult Success() => new(LoginStatus.Ok);
    public static LoginResult Invalid() => new(LoginStatus.Invalid);
    public static LoginResult Locked(int retryAfterSeconds) => new(LoginStatus.Locked, retryAfterSeconds);
    public static LoginResult Throttled(int retryAfterSeconds) => new(LoginStatus.Throttled, retryAfterSeconds);
    public static LoginResult Error() => new(LoginStatus.Error);
}
