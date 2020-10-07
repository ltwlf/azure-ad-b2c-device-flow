namespace Ltwlf.Azure.B2C
{
    public class ConfigOptions
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string Tenant { get; set; }
        public string RedirectUri { get; set; }
        public string SignInPolicy { get; set; }
        public string VerificationUri { get; set; }
        public string Redis { get; set; }
        public int ExpiresIn { get; set; } = 300;
        public int UserCodeLength { get; set; } = 6;
    }
}