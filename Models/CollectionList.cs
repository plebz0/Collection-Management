using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.IO;

namespace Collection_Management.Models
{
    public class CollectionList : INotifyPropertyChanged
    {
        public ObservableCollection<Collection> Collections { get; set; }
        private string DataPath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public CollectionList()
        {
            Collections = new ObservableCollection<Collection>();
            InitializeDataPath();
        }

        private void InitializeDataPath()
        {
            DataPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "CollectionManagement",
                "Data"
            );

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Data path: {DataPath}");
        }

        //Load
        public void LoadCollections()
        {
            try
            {
                Collections.Clear();
                if (!Directory.Exists(DataPath))
                {
                    return;
                }

                string[] collectionFiles = Directory.GetFiles(DataPath, "*.txt");

                foreach (string file in collectionFiles)
                {
                    string collectionName = Path.GetFileNameWithoutExtension(file);
                    Collection collection = new Collection { Name = collectionName };

                    string[] lines = File.ReadAllLines(file);
                    if (lines.Length > 0)
                    {
                        // First line contains collection metadata (Name|Type|Properties|EnumValues)
                        collection = Collection.FromString(lines[0]);

                        // Remaining lines are items
                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(lines[i]))
                            {
                                collection.Items.Add(Item.FromString(lines[i]));
                            }
                        }
                    }

                    Collections.Add(collection);
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Załadowano {Collections.Count} kolekcji");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Błąd podczas ładowania kolekcji: {ex.Message}");
            }
        }

  
        //public async Task StartLoadFromFile()
        //{
        //    var result = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Wybierz note.txt" });
        //    if (result == null) return;

        //    var temp = Path.Combine(FileSystem.CacheDirectory, result.FileName);
        //    using (var stream = await result.OpenReadAsync())
        //    using (var fs = File.Create(temp))
        //        await stream.CopyToAsync(fs);

        //    this.LoadCollections(temp);
        //}

        //Save
        public string StartSaveToFile()
        {
            this.SaveCollections(DataPath);
            return DataPath;
        }

        public void SaveCollections(string path = null)
        {
            try
            {
                string savePath = path ?? DataPath;

                foreach (Collection collection in Collections)
                {
                    string filePath = Path.Combine(savePath, $"{collection.Name}.txt");

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Zapisz metadane kolekcji
                        writer.WriteLine(collection.ToString());

                        // Zapisz elementy
                        foreach (Item item in collection.Items)
                        {
                            writer.WriteLine(item.ToString());
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Zapisano {Collections.Count} kolekcji do: {savePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Błąd podczas zapisywania kolekcji: {ex.Message}");
            }
        }

        public async Task StartLoadFromFile()
        {
            LoadCollections();
            await Task.CompletedTask;
        }

        //Everything else

        public void AddCollection(Collection collection)
        {
            Collections.Add(collection);
            SaveCollections();
        }

        public void RemoveCollection(Collection collection)
        {
            Collections.Remove(collection);
            string filePath = Path.Combine(DataPath, $"{collection.Name}.txt");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            SaveCollections();
        }

        public void UpdateCollection(Collection collection)
        {
            int index = Collections.IndexOf(collection);
            if (index >= 0)
            {
                Collections[index] = collection;
                SaveCollections();
            }
            OnPropertyChanged(nameof(Collections));
        }

        public void AddItemToCollection(Collection collection, Item item)
        {
            if (collection != null)
            {
                // Ensure item has all collection properties with default values
                foreach (var prop in collection.PropertiesTypes)
                {
                    if (!item.Properties.Exists(p => p.Name == prop.Key))
                    {
                        item.Properties.Add(collection.CreatePropertyWithDefaultValue(prop.Key, prop.Value));
                    }
                }

                collection.Items.Add(item);
                SaveCollections();
            }
        }

        public void RemoveItemFromCollection(Collection collection, Item item)
        {
            if (collection != null && collection.Items.Contains(item))
            {
                collection.Items.Remove(item);
                SaveCollections();
            }
        }

        public void UpdateItemInCollection(Collection collection, Item oldItem, Item newItem)
        {
            if (collection != null)
            {
                int index = collection.Items.IndexOf(oldItem);
                if (index >= 0)
                {
                    collection.Items[index] = newItem;
                    SaveCollections();
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 
