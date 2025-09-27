namespace PigFarmManagement.Server.Infrastructure.Settings
{
    public class PosposOptions
    {
        // Base URL for the POSPOS API. Can be set via environment variable POSPOS_API_BASE
        public string ApiBase { get; set; } = string.Empty;

        // API key for POSPOS. Should be provided via environment variable POSPOS_API_KEY
        public string ApiKey { get; set; } = string.Empty;
    }
}
