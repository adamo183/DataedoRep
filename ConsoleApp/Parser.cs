namespace ConsoleApp
{
    using Microsoft.VisualBasic.FileIO;
    using System;
    using System.Collections.Generic;
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
                        x.Type == match.ParentType);

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
                importedObject.ParentType = importedObject.ParentType.Clear();
            }
        }

        internal void Import(string fileToImport)
        {
            this.ImportedObjects = new List<ImportedObject>();

            var streamReader = new StreamReader(fileToImport);

            var importedLines = new List<string>();

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                importedLines.Add(line);
            }

            for (int i = 1; i < importedLines.Count; i++)
            {
                var importedLine = importedLines[i];
                var values = importedLine.Split(';');
                var importedObject = new ImportedObject
                {
                    Type = values?[0],
                    Name = values?[1],
                    Schema = values?[2],
                    ParentName = values?[3],
                    ParentType = values?[4],
                    ParentSchema = values?[5],
                    Title = values?[6],
                    Description = values?[7],
                    CustomField1 = values?[8],
                    CustomField2 = values?[9],
                    CustomField3 = values?[10]
                };

                this.ImportedObjects.Add(importedObject);
            }

            foreach (var importedObject in this.ImportedObjects)
            {
                importedObject.Type = importedObject.Type.Clear().ToUpper();
                importedObject.Name = importedObject.Name.Clear();
                importedObject.Schema = importedObject.Schema.Clear();
                importedObject.ParentName = importedObject.ParentName.Clear();
                importedObject.ParentType = importedObject.ParentType.Clear();
                importedObject.ParentSchema = importedObject.ParentSchema.Clear();
            }
        }
    }
}
