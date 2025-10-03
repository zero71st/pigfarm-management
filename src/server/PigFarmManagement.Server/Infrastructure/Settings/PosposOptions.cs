namespace PigFarmManagement.Server.Infrastructure.Settings
{
    public class PosposOptions
    {
        // Base URL for the POSPOS Stock/Product API. Can be set via environment variable POSPOS_PRODUCT_API_BASE
        public string ProductApiBase { get; set; } = string.Empty;

        // Member/Customer-specific endpoint. Can be set via environment variable POSPOS_MEMBER_API_BASE
        public string MemberApiBase { get; set; } = string.Empty;

        // Transactions-specific endpoint. If provided, this will be used for fetching transactions.
        // Can be set via environment variable POSPOS_TRANSACTIONS_API_BASE or configuration key Pospos:TransactionsApiBase
        public string TransactionsApiBase { get; set; } = string.Empty;

        // API key for POSPOS. Should be provided via environment variable POSPOS_API_KEY
        public string ApiKey { get; set; } = string.Empty;
    }
}
