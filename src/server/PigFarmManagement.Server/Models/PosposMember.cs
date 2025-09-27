namespace PigFarmManagement.Server.Models
{
    public class PosposMember
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public long createdAt { get; set; }
    }
}
