using System;
using System.Xml.Serialization;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CSVXMLJSON
{
   public class Entry
    {
        public string Category { get; set; }
        public bool ForAll { get; set; }
        public string ParentCategory { get; set; }
        public List<string> Values { get; set; }
    }

    public class Category
    {
        public string Name { get; set; }

        [XmlArrayItem("Category", IsNullable = false)]
        public Category[] Children { get; set; }

        [XmlAttribute("everyone")]
        public bool Everyone { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var ser = new XmlSerializer(typeof(Category));
            Category rootCategory;
            using (var s = File.OpenText(@"XMLTZ.xml"))
                rootCategory = (Category)ser.Deserialize(s);

            Dictionary<string, Entry> entries = new();
            FillDictionary(rootCategory, null);

            void FillDictionary(Category category, string parentName)
            {
                var entry = new Entry()
                {
                    ParentCategory = parentName,
                    Category = category.Name,
                    ForAll = category.Everyone
                };
                var fullName = parentName == null ? category.Name : $"{parentName}.{category.Name}";
                entries[fullName] = entry;
                if (category.Children != null)
                    foreach (var childCategory in category.Children)
                        FillDictionary(childCategory, fullName);
            }

            using (var csv = new Microsoft.VisualBasic.FileIO.TextFieldParser(@"CSVTZ.csv"))
            {
                csv.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                csv.SetDelimiters(";");
                while (!csv.EndOfData)
                {
                    string[] fields = csv.ReadFields();
                    var categoryName = fields[0];
                    var modelName = fields[1];

                    if (!entries.TryGetValue(categoryName, out var entry))
                        throw new Exception($"Unexpected category: {categoryName}");

                    entry.Values ??= new();
                    entry.Values.Add(modelName);
                }
            }

            var json = JsonSerializer.Serialize(
                entries.Values.Where(v => v.Values != null),
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            Console.WriteLine(json);
            using (var stream = File.Create(@"JSONTZ.json"))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                JsonSerializer.Serialize(
                    writer,
                    entries.Values.Where(v => v.Values != null),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}
