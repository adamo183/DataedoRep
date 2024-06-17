namespace ConsoleApp
{
    using CsvHelper;
    using Microsoft.VisualBasic.FileIO;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Policy;

    public class Parser
    {
        ILogger logger;
        public Parser(ILogger _logger)
        {
            logger = _logger;
        }

        private IList<ImportedObject> ImportedObjects;
        private IList<DataSourceObject> DataSource;

        public void Do(string fileToImport, string dataSource)
        {
            Import(fileToImport);
            Load(dataSource);
            MatchAndUpdate();
            Print();
        }

        private void Print()
        {
            foreach (var dataSourceObject in this.DataSource.OrderBy(x => x.Type))
            {
                switch (dataSourceObject.Type)
                {
                    case "DATABASE":
                    case "GLOSSARY":
                    case "DOMAIN":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{dataSourceObject.Type} '{dataSourceObject.Name} ({dataSourceObject.Title})'");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(dataSourceObject.Description);
                        Console.ResetColor();

                        // direct children of database like tables, procedures, lookups
                        var childrenGroups = this.DataSource
                            .Where(x =>
                                x.ParentId == dataSourceObject.Id &&
                                x.ParentType == dataSourceObject.Type)
                            .GroupBy(x => x.Type);

                        foreach (var childrenGroup in childrenGroups)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"\t{childrenGroup.Key}S ({childrenGroup.Count()}):");
                            Console.ResetColor();

                            foreach (var child in childrenGroup.OrderBy(x => x.Name))
                            {
                                // direct sub children like columns, parameters, values
                                var subChildrenGroups = this.DataSource
                                    .Where(x =>
                                        x.ParentId == child.Id &&
                                        x.ParentType == child.Type)
                                    .GroupBy(x => x.Type);

                                Console.WriteLine($"\t\t{child.Schema}.{child.Name} ({child.Title})");
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"\t\t{child.Description}");
                                Console.ResetColor();

                                foreach (var subChildrenGroup in subChildrenGroups)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine($"\t\t\t{subChildrenGroup.Key}S ({subChildrenGroup.Count()}):");
                                    Console.ResetColor();

                                    foreach (var subChild in subChildrenGroup.OrderBy(x => x.Name))
                                    {
                                        Console.WriteLine($"\t\t\t\t{subChild.Name} ({subChild.Title})");
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                        Console.WriteLine($"\t\t\t\t{subChild.Description}");
                                        Console.ResetColor();
                                    }
                                }
                            }
                        }

                        break;
                }
            }

            Console.ReadKey();
        }

        private void MatchAndUpdate()
        {
            foreach (var importedObject in this.ImportedObjects)
            {
                var match = this.DataSource.FirstOrDefault(x =>
                    x.Type == importedObject.Type &&
                    x.Name == importedObject.Name &&
                    x.Schema == importedObject.Schema);

                if (match is null)
                {
                    logger.Log(importedObject.ToString());
                    continue;
                }

                if (match.ParentId > 0 && !string.IsNullOrEmpty(importedObject.ParentType))
                {
                    var parent = this.DataSource.FirstOrDefault(x =>
                        x.Id == match.ParentId &&
                        x.Type == importedObject.ParentType);

                    //zakomentuje bo nie rozumiem o jaki jest sens tego porównania
                    //możliwe że tylko logujemy jeśli parent is null ? 
                    //if (parent?.Name != importedObject.ParentName
                    //    || parent?.Schema != importedObject.ParentSchema
                    //    || parent?.Type != importedObject.ParentType)
                    if (parent is null)
                    {
                        logger.Log($"No parent: {importedObject.ToString()}");
                        continue;
                    }

                    parent.Title = importedObject.Title;
                    parent.Description = importedObject.Description;
                    parent.CustomField1 = importedObject.CustomField1;
                    parent.CustomField2 = importedObject.CustomField2;
                    parent.CustomField3 = importedObject.CustomField3;

                }

                match.Title = importedObject.Title;
                match.Description = importedObject.Description;
                match.CustomField1 = importedObject.CustomField1;
                match.CustomField2 = importedObject.CustomField2;
                match.CustomField3 = importedObject.CustomField3;
            }
        }

        private void Load(string dataSource)
        {
            this.DataSource = new List<DataSourceObject>();

            var parser = new TextFieldParser(dataSource);
            parser.SetDelimiters(new string[] { ";" });
            parser.ReadLine();

            while (!parser.EndOfData)
            {
                string[] values = parser.ReadFields();
                var dataSourceObject = new DataSourceObject
                {
                    Id = Convert.ToInt32(values[0]),
                    Type = values[1],
                    Name = values[2],
                    Schema = values[3],
                    ParentId = !string.IsNullOrEmpty(values[4]) ? Convert.ToInt32(values[4]) : 1,
                    ParentType = values[5],
                    Title = values[6],
                    Description = values[7],
                    CustomField1 = values[8],
                    CustomField2 = values[9],
                    CustomField3 = values[10]
                };

                this.DataSource.Add(dataSourceObject);
            }

            foreach (var importedObject in this.DataSource)
            {
                importedObject.Type = importedObject.Type.Clear().ToUpper();
                importedObject.Name = importedObject.Name.Clear();
                importedObject.Schema = importedObject.Schema.Clear();
                importedObject.ParentType = importedObject.ParentType.Clear().ToUpper();
            }
        }

        internal void Import(string fileToImport)
        {
            this.ImportedObjects = new List<ImportedObject>();
            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                MissingFieldFound = null,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                HeaderValidated = null
            };


            using (var reader = new StreamReader(fileToImport))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var importedObject = new ImportedObject()
                    {
                        Type = csv.GetField<string>("Type"),
                        Name = csv.GetField<string>("Name"),
                        Schema = csv.GetField<string>("Schema"),
                        ParentName = csv.GetField<string>("ParentName"),
                        ParentType = csv.GetField<string>("ParentType"),
                        ParentSchema = csv.GetField<string>("ParentSchema"),
                        Title = csv.GetField<string>("Title"),
                        Description = csv.GetField<string>("Description"),
                        CustomField1 = csv.GetField<string>("CustomField1"),
                        CustomField2 = csv.GetField<string>("CustomField2"),
                        CustomField3 = csv.GetField<string>("CustomField3")
                    };
                    ImportedObjects.Add(importedObject);
                }
            }

            foreach (var importedObject in ImportedObjects)
            {
                importedObject.Type = importedObject.Type.Clear().ToUpper();
                importedObject.Name = importedObject.Name.Clear();
                importedObject.Schema = importedObject.Schema.Clear();
                importedObject.ParentName = importedObject.ParentName.Clear();
                importedObject.ParentType = importedObject.ParentType.Clear().ToUpper();
                importedObject.ParentSchema = importedObject.ParentSchema.Clear();
            }
        }
    }
}
