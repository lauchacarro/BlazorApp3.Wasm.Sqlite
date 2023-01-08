namespace BlazorApp3.Wasm.Sqlite.Entities
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime Birthday { get; set; }
        public bool IsActive { get; set; }
    }
}
