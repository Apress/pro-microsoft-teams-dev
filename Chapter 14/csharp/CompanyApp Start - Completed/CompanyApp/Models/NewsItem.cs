namespace CompanyApp.Models
{
    public class NewsItem
    {
        public int Id { get; set; }
        public Location Location { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }

        public NewsItem(int id, Location location, string title, string contents)
        {
            Id = id;
            Title = title;
            Contents = contents;
            Location = location;
        }
    }
}
