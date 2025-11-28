public static class UserSession
{
    private static string _nickname;

    public static string Nickname
    {
        get => _nickname;
        set => _nickname = value?.Trim();
    }
    public static string Username { get; set; }
    public static bool IsGuest { get; set; }
}