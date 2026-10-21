namespace CineSpace.Server.Options;
public sealed class GoogleOptions
{
    public const string Section = "Authentication:Google";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
