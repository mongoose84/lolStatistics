namespace RiotProxy.External.Domain.Entities
{
    public class Gamer : EntityBase
    {
        public string Puuid { get; set; } = string.Empty;
        public string GamerName { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public int IconId { get; set; }
        public string IconUrl { get; set; } = string.Empty;
        public long Level { get; set; }
        public DateTime LastChecked { get; set; }
        public GamerStats Stats { get; set; } = new GamerStats();
    }
}