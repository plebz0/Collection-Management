namespace Collection_Management;

using Collection_Management.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

public partial class CollectionDetailPage : ContentPage
{
    private Collection collection;
    private CollectionList collectionList;
    private Item selectedItem;

    // Constructor - initializes page, sets collection and subscribes to item status changes
    public CollectionDetailPage(Collection collection, CollectionList collectionList)
    {
        InitializeComponent();
        this.collection = collection;
        this.collectionList = collectionList;

        BindingContext = new { Collection = collection };

        // Subscribe to property changes for all items
        foreach (var item in collection.Items)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }

        // Sort items with sold items at the end and bind
        UpdateItemsDisplay();
    }

    // Handles item status change - saves collection and re-sorts list
    private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Item.Status))
        {
            // Save collection and re-sort items
            collectionList.SaveCollections();
            UpdateItemsDisplay();
        }
    }

    // Sorts items (sold at end) and refreshes display in CollectionView
    private void UpdateItemsDisplay()
    {
        var sortedItems = collection.Items
            .OrderBy(i => i.Status == ItemStatus.Sold ? 1 : 0)
            .ThenBy(i => i.Name)
            .ToList();

        ItemsCollectionView.ItemsSource = new ObservableCollection<Item>(sortedItems);
    }

    // Adds new item - asks for name, description, quantity, condition, picture and properties
    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Nowy Element", "Podaj nazwę elementu:", "OK", "Anuluj");
        if (string.IsNullOrWhiteSpace(name))
            return;

        if(checkForDuplicateItem(name))
            if(await GetAnswerForDuplicateItem())
                return;

        string description = await DisplayPromptAsync("Nowy Element", "Podaj opis (opcjonalnie):", "OK", "Anuluj");

        string quantityStr = await DisplayPromptAsync("Nowy Element", "Podaj ilość:", "OK", "Anuluj", "1");
        int quantity = int.TryParse(quantityStr, out int qty) ? qty : 1;

        string condition = await DisplayActionSheet("Nowy Element", "Anuluj", null,"Brand new", "Good", "Fair", "Poor");
        if (condition == "Anuluj")
            condition = "Good";

        Item newItem = new Item(name, description ?? "", quantity, condition);

        // Add default custom properties to new item
        foreach (var prop in collection.PropertiesTypes)
        {
            newItem.Properties.Add(collection.CreatePropertyWithDefaultValue(prop.Key, prop.Value));
        }

        // Ask if user wants to add a picture
        bool addPicture = await DisplayAlert("Zdjęcie", "Czy chcesz dodać zdjęcie do elementu?", "Tak", "Nie");
        if (addPicture)
        {
            await PickAndSetItemPicture(newItem);
        }

        // If collection has custom properties, automatically show edit dialog
        if (collection.PropertiesTypes.Count > 0)
        {
            await EditItemCustomProperties(newItem);
        }

        collectionList.AddItemToCollection(collection, newItem);
        newItem.PropertyChanged += Item_PropertyChanged;
        UpdateItemsDisplay();
    }

    // Shows dialog to edit item properties - loop allows editing multiple properties
    private async Task EditItemCustomProperties(Item item)
    {
        bool editMore = true;
        while (editMore)
        {
            // Only show custom properties (from collection.PropertiesTypes)
            var customProps = item.Properties.Where(p => collection.PropertiesTypes.ContainsKey(p.Name)).ToList();

            string[] options = customProps.Select(p => $"{p.Name}: {p.Value}").ToArray();
            var menuOptions = options.ToList();
            menuOptions.Add("Gotowe");

            string selected = await DisplayActionSheet("Uzupełnij właściwości elementu", "Anuluj", null, menuOptions.ToArray());

            if (selected == "Anuluj" || selected == "Gotowe")
            {
                editMore = false;
            }
            else
            {
                
                // Edytuj wybraną właściwość
                int propIndex = customProps.FindIndex(p => $"{p.Name}: {p.Value}" == selected);
                if (propIndex >= 0)
                {
                    await EditProperty(item, customProps[propIndex]);
                }
            }
        }
    }

    // Asks user if they want duplicate item with same name
    private async Task<bool> GetAnswerForDuplicateItem()
    {
        return await DisplayAlert("Uwaga", "Element o tej nazwie już istnieje w kolekcji. Czy napewno chcesz mieć powtórkę", "Nie", "Tak");
    }

    // Checks if item with given name already exists in collection
    private bool checkForDuplicateItem(string name)
    {
        bool hasDuplicate = collection.Items.Any(item => 
            item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        // Return true if has duplicate and user didn't confirm it
        return hasDuplicate;
    }

    // Edits existing item - asks for new name, description, quantity, condition and properties
    private async void OnEditItemClicked(object sender, EventArgs e)
    {
        if (ItemsCollectionView.SelectedItem is Item item)
        {
            selectedItem = item;

            string name = await DisplayPromptAsync("Edytuj Element", "Podaj nową nazwę:", "OK", "Anuluj", item.Name);
            if (string.IsNullOrWhiteSpace(name))
                return;

            string description = await DisplayPromptAsync("Edytuj Element", "Podaj nowy opis:", "OK", "Anuluj", item.Description ?? "");

            string quantityStr = await DisplayPromptAsync("Edytuj Element", "Podaj nową ilość:", "OK", "Anuluj", item.Quantity.ToString());
            int quantity = int.TryParse(quantityStr, out int qty) ? qty : item.Quantity;

            string condition = await DisplayActionSheet("Edytuj Element", "Anuluj", null, "Brand new", "Good", "Fair", "Poor");
            if (condition == "Anuluj")
                condition = item.Condition;

            Item updatedItem = new Item(name, description, quantity, condition);

            // Copy image from original item
            updatedItem.imgBlob = item.imgBlob;

            // Copy status from original item
            updatedItem.Status = item.Status;

            // Copy existing properties
            updatedItem.Properties = new List<Property>(item.Properties);

            // Add missing properties with default values (if added to collection)
            foreach (var prop in collection.PropertiesTypes)
            {
                if (!updatedItem.Properties.Exists(p => p.Name == prop.Key))
                {
                    updatedItem.Properties.Add(collection.CreatePropertyWithDefaultValue(prop.Key, prop.Value));
                }
            }

            // Allow editing properties
            await EditItemCustomProperties(updatedItem);

            collectionList.UpdateItemInCollection(collection, item, updatedItem);
            updatedItem.PropertyChanged += Item_PropertyChanged;
            UpdateItemsDisplay();
        }
        else
        {
            await DisplayAlert("Błąd", "Wybierz element do edycji", "OK");
        }
    }

    // Edits or deletes item property - handles Enum, String and Number
    private async Task EditProperty(Item item, Property prop)
    {
        string action = await DisplayActionSheet($"Edytuj: {prop.Name}", "Anuluj", null, "Edytuj wartość", "Usuń");

        if (action == "Edytuj wartość")
        {
            string newValue;

            // If property is Enum type, show options to choose from
            if (prop.Type == PropertyType.Enum && collection.EnumPropertiesValues.TryGetValue(prop.Name, out var enumOptions))
            {
                newValue = await DisplayActionSheet($"Wybierz wartość dla {prop.Name}", "Anuluj", null, enumOptions.ToArray());
                if (newValue == "Anuluj")
                    return;
            }
            else
            {
                string prompt = prop.Type switch
                {
                    PropertyType.Number => "Podaj liczbę:",
                    PropertyType.String => "Podaj tekst:",
                    _ => "Podaj wartość:"
                };

                newValue = await DisplayPromptAsync($"Edytuj {prop.Name}", prompt, "OK", "Anuluj", prop.Value ?? "");
            }

            if (!string.IsNullOrWhiteSpace(newValue))
            {
                prop.Value = newValue;
            }
        }
        else if (action == "Usuń")
        {
            bool confirm = await DisplayAlert("Potwierdzenie", $"Usunąć właściwość \"{prop.Name}\"?", "Tak", "Nie");
            if (confirm)
            {
                item.Properties.Remove(prop);
            }
        }
    }

    // Deletes selected item - asks for confirmation
    private async void OnDeleteItemClicked(object sender, EventArgs e)
    {
        if (ItemsCollectionView.SelectedItem is Item item)
        {
            bool confirm = await DisplayAlert("Potwierdzenie", 
                $"Czy na pewno chcesz usunąć \"{item.Name}\"?", 
                "Tak", "Nie");
            if (confirm)
            {
                collectionList.RemoveItemFromCollection(collection, item);
                UpdateItemsDisplay();
            }
        }
        else
        {
            await DisplayAlert("Błąd", "Wybierz element do usunięcia", "OK");
        }
    }

    // Shows context menu for item - options: edit, change picture, delete
    private async void OnItemMenuClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.BindingContext is Item item)
        {
            string action = await DisplayActionSheet("Opcje", "Anuluj", null, "Edytuj", "Zmień zdjęcie", "Usuń");

            if (action == "Edytuj")
            {
                ItemsCollectionView.SelectedItem = item;
                OnEditItemClicked(null, null);
            }
            else if (action == "Zmień zdjęcie")
            {
                await PickAndSetItemPicture(item);
            }
            else if (action == "Usuń")
            {
                ItemsCollectionView.SelectedItem = item;
                OnDeleteItemClicked(null, null);
            }
        }
    }

    // Called when returning to page - refreshes item display
    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateItemsDisplay();
    }

    // Opens File Picker to select picture, converts to Base64 and assigns to item
    private async Task PickAndSetItemPicture(Item item)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Wybierz zdjęcie",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                item.AddPicture(result.FullPath);
                collectionList.SaveCollections();
                UpdateItemsDisplay();
                await DisplayAlert("Sukces", "Zdjęcie zostało dodane", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Nie udało się dodać zdjęcia: {ex.Message}", "OK");
        }
    }

    // Manages collection properties - adding, editing and deleting properties
    private async void OnManageCollectionPropertiesClicked(object sender, EventArgs e)
    {
        bool managing = true;
        while (managing)
        {
            var options = new List<string> { "Dodaj nową właściwość" };

            // Show existing properties
            foreach (var prop in collection.PropertiesTypes)
            {
                options.Add($"  : {prop.Key}");
            }

            options.Add("Gotowe");

            string selected = await DisplayActionSheet("Zarządzaj właściwościami kolekcji", "Anuluj", null, options.ToArray());

            if (selected == "Anuluj" || selected == "Gotowe")
            {
                managing = false;
                collectionList.UpdateCollection(collection);
            }
            else if (selected == "Dodaj nową właściwość")
            {
                await AddNewCollectionProperty();
            }
            else if (selected.StartsWith("Edytuj: "))
            {
                string propName = selected.Replace("Edytuj: ", "");
                if (collection.PropertiesTypes.TryGetValue(propName, out PropertyType propType))
                {
                    await EditCollectionProperty(propName, propType);
                }
            }
        }
    }

    // Adds new property to collection - type can be String, Number or Enum
    private async Task AddNewCollectionProperty()
    {
        string propName = await DisplayPromptAsync("Nowa właściwość", "Podaj nazwę właściwości:", "OK", "Anuluj");
        if (string.IsNullOrWhiteSpace(propName))
            return;

        string typeChoice = await DisplayActionSheet("Typ właściwości", "Anuluj", null, "Tekst", "Liczba", "Lista wyboru");
        if (typeChoice == "Anuluj")
            return;

        PropertyType propType = typeChoice switch
        {
            "Tekst" => PropertyType.String,
            "Liczba" => PropertyType.Number,
            "Lista wyboru" => PropertyType.Enum,
            _ => PropertyType.String
        };

        collection.AddNewProperty(propName, propType);

        if (propType == PropertyType.Enum)
        {
            string enumValues = await DisplayPromptAsync("Lista wyboru", "Podaj wartości oddzielone przecinkami (np: opcja1,opcja2):", "OK", "Anuluj");
            if (!string.IsNullOrWhiteSpace(enumValues))
            {
                var values = new List<string>(enumValues.Split(',').Select(v => v.Trim()));
                collection.SetEnumPropertyValues(propName, values);
            }
        }

        // Refresh the items display to show new properties
        UpdateItemsDisplay();
    }

    // Edits existing property - change type or delete
    private async Task EditCollectionProperty(string propName, PropertyType propType)
    {
        string action = await DisplayActionSheet($"Edytuj: {propName}", "Anuluj", null, "Zmień typ", "Usuń");

        if (action == "Zmień typ")
        {
            string typeChoice = await DisplayActionSheet("Nowy typ", "Anuluj", null, "Tekst", "Liczba", "Lista wyboru");
            if (typeChoice != "Anuluj")
            {
                PropertyType newType = typeChoice switch
                {
                    "Tekst" => PropertyType.String,
                    "Liczba" => PropertyType.Number,
                    "Lista wyboru" => PropertyType.Enum,
                    _ => PropertyType.String
                };

                collection.ChangePropertyType(propName, newType);

                if (newType == PropertyType.Enum)
                {
                    string enumValues = await DisplayPromptAsync("Lista wyboru", "Podaj wartości oddzielone przecinkami:", "OK", "Anuluj");
                    if (!string.IsNullOrWhiteSpace(enumValues))
                    {
                        var values = new List<string>(enumValues.Split(',').Select(v => v.Trim()));
                        collection.SetEnumPropertyValues(propName, values);
                    }
                }

                // Refresh the items display
                UpdateItemsDisplay();
            }
        }
        else if (action == "Usuń")
        {
            bool confirm = await DisplayAlert("Potwierdzenie", $"Usunąć właściwość \"{propName}\" ze wszystkich elementów?", "Tak", "Nie");
            if (confirm)
            {
                collection.RemoveProperty(propName);

                // Refresh the items display
                UpdateItemsDisplay();
            }
        }
    }

    // Opens collection summary page - shows statistics
    private async void OnSummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CollectionSummaryPage(collection));
    }

    // Returns to previous page (MainPage)
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}