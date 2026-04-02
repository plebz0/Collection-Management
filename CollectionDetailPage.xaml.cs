namespace Collection_Management;

using Collection_Management.Models;

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
        ItemsCollectionView.ItemsSource = collection.Items;
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Nowy Element", "Podaj nazwę elementu:");
        //checkForDuplicateItem(name);
        if (string.IsNullOrWhiteSpace(name))
            return;

        string description = await DisplayPromptAsync("Nowy Element", "Podaj opis (opcjonalnie):", "OK");

        string quantityStr = await DisplayPromptAsync("Nowy Element", "Podaj ilość:", "OK");
        int quantity = int.TryParse(quantityStr, out int qty) ? qty : 1;

        string condition = await DisplayActionSheet("Nowy Element", "Anuluj", null,"Brand new", "Good", "Fair", "Poor");
        if (condition == "Anuluj")
            condition = "Good";

        Item newItem = new Item(name, description ?? "", quantity, condition);

        // Dodaj możliwość dodania właściwości dla nowego elementu
        bool addProps = await DisplayAlert("Właściwości", "Chcesz dodać własne właściwości do tego elementu?", "Tak", "Nie");
        if (addProps)
        {
            await EditItemProperties(newItem);
        }

        collectionList.AddItemToCollection(collection, newItem);
    }

   private void checkForDuplicateItem(string name)
    {
        foreach (var item in collection.Items)
        {
            if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                DisplayAlert("Błąd", "Element o tej nazwie już istnieje w kolekcji. Czy napewno chcesz mieć powtórkę", "Tak", "Nie");
                return;
            }
        }
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

            string condition = await DisplayActionSheet("Edytuj Element", "Anuluj", null, "Mint", "Good", "Fair", "Poor");
            if (condition == "Anuluj")
                condition = item.Condition;

            Item updatedItem = new Item(name, description, quantity, condition);

            // Kopiuj istniejące właściwości
            updatedItem.Properties = new List<Property>(item.Properties);

            // Pozwól edytować właściwości
            await EditItemProperties(updatedItem);

            collectionList.UpdateItemInCollection(collection, item, updatedItem);
        }
        else
        {
            await DisplayAlert("Błąd", "Wybierz element do edycji", "OK");
        }
    }

    private async Task EditItemProperties(Item item)
    {
        bool editMore = true;
        while (editMore)
        {
            string[] options = item.Properties.Select(p => $"{p.Name}: {p.Value}").ToArray();
            var menuOptions = options.ToList();
            menuOptions.Add("Dodaj nową właściwość");
            menuOptions.Add("Gotowe");

            string selected = await DisplayActionSheet("Zarządzaj właściwościami", "Anuluj", null, menuOptions.ToArray());

            if (selected == "Anuluj" || selected == "Gotowe")
            {
                editMore = false;
            }
            else if (selected == "Dodaj nową właściwość")
            {
                await AddNewProperty(item);
            }
            else
            {
                // Edytuj istniejącą właściwość
                int propIndex = item.Properties.FindIndex(p => $"{p.Name}: {p.Value}" == selected);
                if (propIndex >= 0)
                {
                    await EditProperty(item, item.Properties[propIndex]);
                }
            }
        }
    }

    private async Task AddNewProperty(Item item)
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

        Property newProp = new Property(propName, propType);

        // Ustaw wartość w zależności od typu
        string value = null;
        if (propType == PropertyType.Enum)
        {
            // Jeśli kolekcja ma zdefiniowane wartości enum dla tej właściwości
            if (collection.EnumPropertiesValues.TryGetValue(propName, out var enumOptions) && enumOptions.Count > 0)
            {
                value = await DisplayActionSheet($"Wybierz wartość dla {propName}", "Anuluj", null, enumOptions.ToArray());
                if (value == "Anuluj")
                    return;
            }
            else
            {
                value = await DisplayPromptAsync($"Wartość dla {propName}", "Podaj wartość:", "");
            }
        }
        else
        {
            value = typeChoice switch
            {
                "Tekst" => await DisplayPromptAsync($"Wartość dla {propName}", "Podaj tekst:", ""),
                "Liczba" => await DisplayPromptAsync($"Wartość dla {propName}", "Podaj liczbę:", "0"),
                _ => ""
            };
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            newProp.Value = value;
            item.Properties.Add(newProp);
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
            string action = await DisplayActionSheet("Opcje", "Anuluj", null, "Edytuj", "Usuń");
            
            if (action == "Edytuj")
            {
                ItemsCollectionView.SelectedItem = item;
                OnEditItemClicked(null, null);
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
        ItemsCollectionView.ItemsSource = collection.Items;
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
            string enumValues = await DisplayPromptAsync("Lista wyboru", "Podaj wartości oddzielone przecinkami (np: opcja1,opcja2):", "");
            if (!string.IsNullOrWhiteSpace(enumValues))
            {
                var values = new List<string>(enumValues.Split(',').Select(v => v.Trim()));
                collection.EnumPropertiesValues[propName] = values;
            }
        }
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

                collection.PropertiesTypes[propName] = newType;

                if (newType == PropertyType.Enum)
                {
                    string enumValues = await DisplayPromptAsync("Lista wyboru", "Podaj wartości oddzielone przecinkami:", "");
                    if (!string.IsNullOrWhiteSpace(enumValues))
                    {
                        var values = new List<string>(enumValues.Split(',').Select(v => v.Trim()));
                        collection.EnumPropertiesValues[propName] = values;
                    }
                }
                else if (collection.EnumPropertiesValues.ContainsKey(propName))
                {
                    collection.EnumPropertiesValues.Remove(propName);
                }
            }
        }
        else if (action == "Usuń")
        {
            bool confirm = await DisplayAlert("Potwierdzenie", $"Usunąć właściwość \"{propName}\" ze wszystkich elementów?", "Tak", "Nie");
            if (confirm)
            {
                collection.PropertiesTypes.Remove(propName);
                if (collection.EnumPropertiesValues.ContainsKey(propName))
                {
                    collection.EnumPropertiesValues.Remove(propName);
                }
                foreach (var item in collection.Items)
                {
                    var prop = item.Properties.FirstOrDefault(p => p.Name == propName);
                    if (prop != null)
                    {
                        item.Properties.Remove(prop);
                    }
                }
            }
        }
    }
}
