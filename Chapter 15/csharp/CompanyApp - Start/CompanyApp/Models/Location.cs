namespace CompanyApp.Models
{
    public enum Location
    {
        America,
        Europe,
        Asia
    }

    public class LocationModel
    {
        public Location Location { get; set; }
    }
}
