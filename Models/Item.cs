using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Collection_Management.Models
{
    public enum ItemStatus
    {
        Possess,
        WantToSell,
        Sold
    }

    public class Item : INotifyPropertyChanged
    {
        private ItemStatus _status;
        public ItemStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        private string _name;
        public string Name { 
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _description;
        public string Description { 
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        private int _quantity;
        public int Quantity { 
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                }
            }
        }

        private string _condition;
        public string Condition { 
            get => _condition;
            set
            {
                if (_condition != value)
                {
                    _condition = value;
                    OnPropertyChanged(nameof(Condition));
                }
            }
        }


        private string _imgBlob;
        public string imgBlob { 
            get => _imgBlob;
            set
            {
                if (_imgBlob != value)
                {
                    _imgBlob = value;
                    OnPropertyChanged(nameof(imgBlob));
                }
            }
        }

        public string ImageDisplay => string.IsNullOrEmpty(imgBlob) ? "No picture" : "Picture available";

        public bool HasPicture => !string.IsNullOrEmpty(imgBlob);

        public List<Property> Properties { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Item(string name, string description = "", int quantity = 1, string condition = "Good")
        {
            Name = name;
            Description = description;
            Quantity = quantity;
            Condition = condition;
            Properties = new List<Property>();
            Status = ItemStatus.Possess;
            imgBlob = string.Empty;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            string baseData = $"{Name}|{Description}|{Quantity}|{Condition}|{Status}|{imgBlob}";

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
            if (parts.Length >= 5)
            {
                ItemStatus status = ItemStatus.Possess;
                if (Enum.TryParse<ItemStatus>(parts[4], out ItemStatus parsedStatus))
                {
                    status = parsedStatus;
                }

                var item = new Item(parts[0], parts[1], int.TryParse(parts[2], out int qty) ? qty : 1, parts[3])
                {
                    Status = status
                };

                // Load image blob if it exists
                if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                {
                    // Check if it looks like a base64 string (not property data)
                    if (!parts[5].Contains(':'))
                    {
                        item.imgBlob = parts[5];
                    }
                    else
                    {
                        // Load custom properties if they exist
                        string[] propsList = parts[5].Split(';');
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
                }

                // Load custom properties if they exist at index 6
                if (parts.Length > 6 && !string.IsNullOrWhiteSpace(parts[6]))
                {
                    string[] propsList = parts[6].Split(';');
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


        public void AddPicture(string filePath)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                imgBlob = Convert.ToBase64String(fileBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading picture: {ex.Message}");
            }
        }

        public void addNewProperty(Property value)
        {
            Properties.Add(value);
            OnPropertyChanged(nameof(Properties));
        }
    }
}
