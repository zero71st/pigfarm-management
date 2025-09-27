namespace PigFarmManagement.Server.Models
{
    public class PosposMember
    {
        public string id { get; set; } = string.Empty;
        // Split name into FirstName and LastName per user request
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public long createdAt { get; set; }
    }
}
