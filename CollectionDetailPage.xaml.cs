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

    private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Item.Status))
        {
            // Save collection and re-sort items
            collectionList.SaveCollections();
            UpdateItemsDisplay();
        }
    }

    private void UpdateItemsDisplay()
    {
        var sortedItems = collection.Items
            .OrderBy(i => i.Status == ItemStatus.Sold ? 1 : 0)
            .ThenBy(i => i.Name)
            .ToList();

        ItemsCollectionView.ItemsSource = new ObservableCollection<Item>(sortedItems);
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Nowy Element", "Podaj nazwę elementu:");
        if (string.IsNullOrWhiteSpace(name))
            return;

        if(checkForDuplicateItem(name))
            if(await GetAnswerForDuplicateItem())
                return;

        string description = await DisplayPromptAsync("Nowy Element", "Podaj opis (opcjonalnie):", "OK");

        string quantityStr = await DisplayPromptAsync("Nowy Element", "Podaj ilość:", "OK");
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

    private async Task<bool> GetAnswerForDuplicateItem()
    {
        return await DisplayAlert("Uwaga", "Element o tej nazwie już istnieje w kolekcji. Czy napewno chcesz mieć powtórkę", "Tak", "Nie");
    }

    //Perzenieś do Models
    private bool checkForDuplicateItem(string name)
    {
        bool hasDuplicate = collection.Items.Any(item => 
            item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        // Zwróć true jeśli ma duplikat I użytkownik go nie potwierdził
        return hasDuplicate;
    }
    private async void OnEditItemClicked(object sender, EventArgs e)
    {
        if (ItemsCollectionView.SelectedItem is Item item)
        {
            selectedItem = item;

            string name = await DisplayPromptAsync("Edytuj Element", $"Nazwa:{item.Name}", "OK");
            if (string.IsNullOrWhiteSpace(name))
                return;

            string description = await DisplayPromptAsync("Edytuj Element", $"Opis:{item.Description ?? ""}", "OK");

            string quantityStr = await DisplayPromptAsync("Edytuj Element", "Ilość:", item.Quantity.ToString());
            int quantity = int.TryParse(quantityStr, out int qty) ? qty : item.Quantity;

            string condition = await DisplayActionSheet("Edytuj Element", "Anuluj", null, "Brand new", "Good", "Fair", "Poor");
            if (condition == "Anuluj")
                condition = item.Condition;

            Item updatedItem = new Item(name, description, quantity, condition);

            // Kopiuj istniejące właściwości
            updatedItem.Properties = new List<Property>(item.Properties);

            // Dodaj brakujące właściwości z domyślnymi wartościami (jeśli zostały dodane do kolekcji)
            foreach (var prop in collection.PropertiesTypes)
            {
                if (!updatedItem.Properties.Exists(p => p.Name == prop.Key))
                {
                    updatedItem.Properties.Add(collection.CreatePropertyWithDefaultValue(prop.Key, prop.Value));
                }
            }

            // Pozwól edytować właściwości
            await EditItemCustomProperties(updatedItem);

            collectionList.UpdateItemInCollection(collection, item, updatedItem);
            UpdateItemsDisplay();
        }
        else
        {
            await DisplayAlert("Błąd", "Wybierz element do edycji", "OK");
        }
    }

    private async Task EditProperty(Item item, Property prop)
    {
        string action = await DisplayActionSheet($"Edytuj: {prop.Name}", "Anuluj", null, "Edytuj wartość", "Usuń");

        if (action == "Edytuj wartość")
        {
            string newValue;

            // Jeśli właściwość ma typ Enum, pokaż opcje do wyboru
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

                newValue = await DisplayPromptAsync($"Edytuj {prop.Name}", prompt, prop.Value ?? "");
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateItemsDisplay();
    }

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

    private async void OnManageCollectionPropertiesClicked(object sender, EventArgs e)
    {
        bool managing = true;
        while (managing)
        {
            var options = new List<string> { "Dodaj nową właściwość" };

            // Pokaż istniejące właściwości
            foreach (var prop in collection.PropertiesTypes)
            {
                options.Add($"Edytuj: {prop.Key}");
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

    private async Task AddNewCollectionProperty()
    {
        string propName = await DisplayPromptAsync("Nowa właściwość", "Podaj nazwę właściwości:");
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
            string enumValues = await DisplayPromptAsync("Lista wyboru", "Podaj wartości oddzielone przecinkami (np: opcja1,opcja2):", "OK");
            if (!string.IsNullOrWhiteSpace(enumValues))
            {
                var values = new List<string>(enumValues.Split(',').Select(v => v.Trim()));
                collection.EnumPropertiesValues[propName] = values; // MODYFIKACJA MODELU!!!!!!
            }
        }

        // Refresh the items display to show new properties
        UpdateItemsDisplay();
    }
    

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

                collection.PropertiesTypes[propName] = newType; // MODYFIKACJA MODELU!!!!!!

                if (newType == PropertyType.Enum)
                {
                    string enumValues = await DisplayPromptAsync("Lista wyboru", "Podaj wartości oddzielone przecinkami:", "");
                    if (!string.IsNullOrWhiteSpace(enumValues))
                    {
                        var values = new List<string>(enumValues.Split(',').Select(v => v.Trim()));
                        collection.EnumPropertiesValues[propName] = values; // MODYFIKACJA MODELU!!!!!!
                    }
                }
                else if (collection.EnumPropertiesValues.ContainsKey(propName))
                {
                    collection.EnumPropertiesValues.Remove(propName); // MODYFIKACJA MODELU!!!!!!
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
                collection.PropertiesTypes.Remove(propName); // MODYFIKACJA MODELU!!!!!!
                if (collection.EnumPropertiesValues.ContainsKey(propName))
                {
                    collection.EnumPropertiesValues.Remove(propName); // MODYFIKACJA MODELU!!!!!!
                }
                foreach (var item in collection.Items)
                {
                    var prop = item.Properties.FirstOrDefault(p => p.Name == propName);
                    if (prop != null)
                    {
                        item.Properties.Remove(prop); // MODYFIKACJA MODELU!!!!!!
                    }
                }

                // Refresh the items display
                UpdateItemsDisplay();
            }
        }
    }

    private async void OnSummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CollectionSummaryPage(collection));
    }
}
