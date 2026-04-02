using System;
using System.Collections.Generic;
using System.Text;

namespace Collection_Management.Models
{
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; }

        public bool IsSold { get; set; }

        public string imgBlob { get; set; }

        public List<Property> Properties { get; set; } 


        public Item(string name, string description = "", int quantity = 1, string condition = "Good")
        {
            Name = name;
            Description = description;
            Quantity = quantity;
            Condition = condition;
            Properties = new List<Property>();
            
        }

        public override string ToString()
        {
            string baseData = $"{Name}|{Description}|{Quantity}|{Condition}";

            if (Properties != null && Properties.Count > 0)
            {
                string propsData = string.Join(";", Properties.Select(p => $"{p.Name}:{p.Type}:{p.Value}"));
                return $"{baseData}|{propsData}";
            }

            return baseData;
        }

        public static Item FromString(string line)
        {
            var parts = line.Split('|');
            if (parts.Length >= 4)
            {
                var item = new Item(parts[0], parts[1], int.TryParse(parts[2], out int qty) ? qty : 1, parts[3]);

                // Wczytaj właściwości jeśli istnieją
                if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
                {
                    string[] propsList = parts[4].Split(';');
                    foreach (string propStr in propsList)
                    {
                        if (!string.IsNullOrWhiteSpace(propStr))
                        {
                            string[] propParts = propStr.Split(':');
                            if (propParts.Length >= 3)
                            {
                                string propName = propParts[0];
                                if (Enum.TryParse<PropertyType>(propParts[1], out PropertyType propType))
                                {
                                    Property prop = new Property(propName, propType);
                                    prop.Value = propParts[2];
                                    item.Properties.Add(prop);
                                }
                            }
                        }
                    }
                }

                return item;
            }
            return new Item(line);
        }

        // public void addPicutre(FileInfo file)
        // {
        //     byte[] fileBytes = File.ReadAllBytes(file.FullName);
        //     imgBlob = Convert.ToBase64String(fileBytes);
        // }
    }
}
