using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Collection_Management.Models
{
    public class Collection : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public ObservableCollection<Item> Items { get; set; }

        public OrderedDictionary<string, List<string>> EnumPropertiesValues { get; set; }
        public OrderedDictionary<string, PropertyType> PropertiesTypes { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Collection()
        {
            Items = new ObservableCollection<Item>();
            EnumPropertiesValues = new OrderedDictionary<string, List<string>>();
            PropertiesTypes = new OrderedDictionary<string, PropertyType>();
        }

        public Collection(string name, string type = "")
        {
            Name = name;
            Type = type;
            Items = new ObservableCollection<Item>();
            EnumPropertiesValues = new OrderedDictionary<string, List<string>>();
            PropertiesTypes = new OrderedDictionary<string, PropertyType>();
        }

        public override string ToString()
        {
            string baseData = $"{Name}|{Type}";

            if (PropertiesTypes != null && PropertiesTypes.Count > 0)
            {
                string propsData = string.Join(";", PropertiesTypes.Select(p => $"{p.Key}:{p.Value}"));

                // Dodaj enum wartości jeśli istnieją
                if (EnumPropertiesValues != null && EnumPropertiesValues.Count > 0)
                {
                    string enumData = string.Join("|", EnumPropertiesValues.Select(e => 
                        $"{e.Key}={string.Join(",", e.Value)}"));
                    return $"{baseData}|{propsData}|{enumData}";
                }

                return $"{baseData}|{propsData}";
            }

            return baseData;
        }

        public static Collection FromString(string line)
        {
            var parts = line.Split('|');
            if (parts.Length >= 2)
            {
                Collection collection = new Collection(parts[0], parts[1]);

                // Wczytaj definicje właściwości jeśli istnieją
                if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
                {
                    string[] propsList = parts[2].Split(';');
                    foreach (string propStr in propsList)
                    {
                        if (!string.IsNullOrWhiteSpace(propStr))
                        {
                            string[] propParts = propStr.Split(':');
                            if (propParts.Length >= 2 && Enum.TryParse<PropertyType>(propParts[1], out PropertyType propType))
                            {
                                collection.PropertiesTypes.Add(propParts[0], propType);
                            }
                        }
                    }
                }

                // Wczytaj enum wartości jeśli istnieją
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]))
                {
                    for(int i = 3; i < parts.Length; i++)
                    {
                        string enumStr = parts[i];
                        if (!string.IsNullOrWhiteSpace(enumStr))
                        {
                            string[] enumParts = enumStr.Split('=');
                            if (enumParts.Length >= 2)
                            {
                                string enumName = enumParts[0];
                                var enumValues = new List<string>(enumParts[1].Split(','));
                                collection.EnumPropertiesValues.Add(enumName, enumValues);
                            }
                        }
                    }

                }

                return collection;
            }
            return new Collection(line);
        }

        public void AddNewProperty(string propertyName, PropertyType propertyType)
        {
            if (!PropertiesTypes.ContainsKey(propertyName))
            {
                PropertiesTypes.Add(propertyName, propertyType);
                if (propertyType == PropertyType.Enum)
                {
                    EnumPropertiesValues.Add(propertyName, new List<string>());
                }
                foreach (var item in Items)
                {
                    item.addNewProperty(CreatePropertyWithDefaultValue(propertyName, propertyType));
                }
            }

        }

        public Property CreatePropertyWithDefaultValue(string propertyName, PropertyType propertyType)
        {
            var property = new Property(propertyName, propertyType);

            // Set default value based on type
            switch (propertyType)
            {
                case PropertyType.String:
                    property.Value = "";
                    break;
                case PropertyType.Number:
                    property.Value = "0";
                    break;
                case PropertyType.Enum:
                    // Use first enum value if available, otherwise empty
                    if (EnumPropertiesValues.TryGetValue(propertyName, out var enumValues) && enumValues.Count > 0)
                    {
                        property.Value = enumValues[0];
                    }
                    else
                    {
                        property.Value = "";
                    }
                    break;
            }

            return property;
        }

        public void AddItem(string name, string description = "", int quantity = 1, string condition = "Good")
        {
            var newItem = new Item(name, description, quantity, condition);

            foreach (var prop in PropertiesTypes)
            {
                newItem.Properties.Add(CreatePropertyWithDefaultValue(prop.Key, prop.Value));
            }

            Items.Add(newItem);
        }
        public void AddItem(Item item)
        {
            foreach (var prop in PropertiesTypes)
            {
                if (!item.Properties.Exists(p => p.Name == prop.Key))
                {
                    item.Properties.Add(CreatePropertyWithDefaultValue(prop.Key, prop.Value));
                }
            }
            Items.Add(item);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
