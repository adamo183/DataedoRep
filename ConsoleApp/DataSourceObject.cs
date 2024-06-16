namespace ConsoleApp
{
    public class DataSourceObject
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Schema { get; set; }

        public int ParentId { get; set; }
        public string ParentType { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string CustomField1 { get; set; }
        public string CustomField2 { get; set; }
        public string CustomField3 { get; set; }
    }
}
