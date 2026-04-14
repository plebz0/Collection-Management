
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Collection_Management.Models
{
    // Manages collections - loads, saves, exports and imports collections and items from files
    public class CollectionList : INotifyPropertyChanged
    {
        // Observable collection of all collections - allows UI binding and automatic updates
        public ObservableCollection<Collection> Collections { get; set; }

        // Path to data directory where collection files are stored
        private string DataPath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        // Constructor - initializes collections and sets data path
        public CollectionList()
        {
            Collections = new ObservableCollection<Collection>();
            InitializeDataPath();
        }

        // Creates data directory if it doesn't exist and sets DataPath property
        // Directory structure: AppDataDirectory/CollectionManagement/Data
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

        // Loads all collection files from data directory and populates the observable Collections
        // Each file should be a .txt file with collection metadata in the first line, then item data
        public void LoadCollections()
        {
            try
            {
                // Clear existing collections
                Collections.Clear();
                if (!Directory.Exists(DataPath))
                {
                    return;
                }

                // Find all .txt files in the data directory
                string[] collectionFiles = Directory.GetFiles(DataPath, "*.txt");

                // Process each collection file
                foreach (string file in collectionFiles)
                {
                    string collectionName = Path.GetFileNameWithoutExtension(file);
                    Collection collection = new Collection { Name = collectionName };

                    string[] lines = File.ReadAllLines(file);
                    if (lines.Length > 0)
                    {
                        // First line contains collection metadata (Name|Type|Properties|EnumValues)
                        collection = Collection.FromString(lines[0]);

                        // Remaining lines contain item data - parse each item
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

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded {Collections.Count} collections");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error loading collections: {ex.Message}");
            }
        }

        // Initiates save operation to default data path
        // Returns path where collections were saved
        public string StartSaveToFile()
        {
            this.SaveCollections(DataPath);
            return DataPath;
        }

        // Saves all collections to .txt files in specified path (or default if not provided)
        // Each collection is saved as: FileName: {CollectionName}.txt
        // Format: First line = metadata, remaining lines = items
        public void SaveCollections(string path = null)
        {
            try
            {
                string savePath = path ?? DataPath;

                // Save each collection to its own file
                foreach (Collection collection in Collections)
                {
                    string filePath = Path.Combine(savePath, $"{collection.Name}.txt");

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Write collection metadata (Name|Type|Properties|EnumValues)
                        writer.WriteLine(collection.ToString());

                        // Write each item in the collection
                        foreach (Item item in collection.Items)
                        {
                            writer.WriteLine(item.ToString());
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Saved {Collections.Count} collections to: {savePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error saving collections: {ex.Message}");
            }
        }

        // Asynchronously loads all collections from file storage
        public async Task StartLoadFromFile()
        {
            LoadCollections();
            await Task.CompletedTask;
        }

        // Exports a single collection to a .txt file using file picker
        // User can choose where to save the file
        // Returns path to exported file, or null if export was cancelled/failed
        public async Task<string> ExportCollectionAsync(Collection collection)
        {
            try
            {
                string fileName = $"{collection.Name}.txt";
                // Save to cache directory with collection name as filename
                string path = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (StreamWriter writer = new StreamWriter(path))
                {
                    // Write collection metadata
                    writer.WriteLine(collection.ToString());

                    // Write each item in the collection
                    foreach (Item item in collection.Items)
                    {
                        writer.WriteLine(item.ToString());
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Collection exported to: {path}");
                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error exporting collection: {ex.Message}");
            }

            return null;
        }

        // Imports a collection from a .txt file selected by user
        // If a collection with same Type already exists, merges imported items into it
        // Otherwise creates new collection with unique name
        // Returns true if import was successful, false if cancelled/failed
        public async Task<bool> ImportCollectionAsync()
        {
            try
            {
                // Open file picker to select a .txt file
                var result = await FilePicker.Default.PickAsync();

                if (result != null)
                {
                    string[] lines = File.ReadAllLines(result.FullPath);
                    if (lines.Length > 0)
                    {
                        // Parse imported collection metadata from first line
                        Collection importedCollection = Collection.FromString(lines[0]);

                        // Parse and add imported items (lines 2 onwards)
                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(lines[i]))
                            {
                                importedCollection.Items.Add(Item.FromString(lines[i]));
                            }
                        }

                        // Check if a collection with the same Type already exists
                        Collection existingCollection = Collections.FirstOrDefault(c => c.Type == importedCollection.Type);

                        if (existingCollection != null)
                        {
                            // Merge imported items into existing collection
                            // This removes imported custom properties and assigns existing collection's properties
                            MergeCollections(existingCollection, importedCollection);
                        }
                        else
                        {
                            // Add as new collection with unique name to avoid duplicates
                            string uniqueName = GetUniqueCollectionName(importedCollection.Name);
                            importedCollection.Name = uniqueName;
                            Collections.Add(importedCollection);
                        }

                        // Persist changes to disk
                        SaveCollections();
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Collection imported: {importedCollection.Name}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error importing collection: {ex.Message}");
            }

            return false;
        }

        // Merges items from imported collection into existing collection of same type
        // Removes all custom properties from imported items to ensure consistency
        private void MergeCollections(Collection existingCollection, Collection importedCollection)
        {
            // Remove all custom properties from imported items to ensure consistency
            foreach (var item in importedCollection.Items)
            {
                item.Properties.Clear();
            }

            // Add items from imported collection with existing collection's properties and default values
            foreach (var item in importedCollection.Items)
            {
                // Assign all properties from the existing collection with their default values
                foreach (var prop in existingCollection.PropertiesTypes)
                {
                    item.Properties.Add(existingCollection.CreatePropertyWithDefaultValue(prop.Key, prop.Value));
                }

                // Add the configured item to the existing collection
                existingCollection.Items.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Merged {importedCollection.Items.Count} items into collection: {existingCollection.Name}");
        }

        // Generates unique collection name by adding counter if name already exists
        // Used when importing collection with duplicate type
        // Returns unique collection name that doesn't exist in Collections
        private string GetUniqueCollectionName(string baseName)
        {
            string name = baseName;
            int counter = 1;

            // Keep incrementing counter until we find an unused name
            while (Collections.Any(c => c.Name == name))
            {
                name = $"{baseName} ({counter})";
                counter++;
            }

            return name;
        }

        // Adds new collection to observable Collections and saves to disk
        public void AddCollection(Collection collection)
        {
            Collections.Add(collection);
            SaveCollections();
        }

        // Removes collection from observable Collections and deletes its file from disk
        public void RemoveCollection(Collection collection)
        {
            Collections.Remove(collection);
            // Delete the associated file
            string filePath = Path.Combine(DataPath, $"{collection.Name}.txt");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            SaveCollections();
        }

        // Updates existing collection in observable Collections and saves to disk
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

        // Adds item to specified collection, ensuring it has all required properties with default values
        public void AddItemToCollection(Collection collection, Item item)
        {
            if (collection != null)
            {
                // Ensure item has all collection properties with default values
                // This prevents missing properties when an item is added to a collection with defined properties
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

        // Removes item from specified collection and saves changes to disk
        public void RemoveItemFromCollection(Collection collection, Item item)
        {
            if (collection != null && collection.Items.Contains(item))
            {
                collection.Items.Remove(item);
                SaveCollections();
            }
        }

        // Replaces old item with new item in collection and saves changes to disk
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

        // Raises PropertyChanged event to notify UI of data changes
        // Used for updating data bindings in MAUI
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
