namespace MKCache.Sample
{
    class Country
    {
        // Id unique in the database.
        public int Id { get; set; }

        public string Name { get; set; }

        // ISO 3166-2 standard, unique identifier.
        public string ISOCode { get; set; }
    }
}
