namespace CompanyApp.Models
{
    public class Lunch
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Lunch(int id, string name)
        {
            Id = id;
            Name = name;
        }

    }
}
