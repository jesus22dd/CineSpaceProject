namespace CineSpace.Server.Options;
public sealed class SupabaseOptions
{
    public const string Section = "Supabase";
    public string ProjectRef { get; set; } = "";
    public string Url { get; set; } = "";
    public string AnonKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "";
}
