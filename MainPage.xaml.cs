namespace Collection_Management;

using Collection_Management.Models;


public partial class MainPage : ContentPage
{
    private CollectionList collectionList;
    private Collection selectedCollection;

    // Constructor - initializes main page, creates collection list and sets data source for CollectionView
    public MainPage()
    {
        InitializeComponent();
        collectionList = new CollectionList();
        CollectionsCollectionView.ItemsSource = collectionList.Collections; // CollectionView control (User Interface)
    }

    // Loads collections from file through FilePicker - displays success or error message
    private async void OnLoadFileClicked(object sender, EventArgs e)
    {
        try
        {
            await collectionList.StartLoadFromFile();

        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", ex.Message, "OK");
        }

    }

    // Saves all collections to file - displays saved file path
    private void OnFileSaveClicked(object sender, EventArgs e)
    {
        try
        {
            var path = collectionList.StartSaveToFile();
            DisplayAlert("Sukces", $"Zapisano do pliku:\n{path}", "OK");
        }
        catch (System.Exception ex)
        {
            DisplayAlert("Błąd", ex.Message, "OK");
        }
    }

    // Adds new collection - asks for name and type, creates new Collection and adds to list
    private async void OnAddCollectionClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Nowa Kolekcja", "Podaj nazwę kolekcji:");
        if (!string.IsNullOrWhiteSpace(name))
        {
            string type = await DisplayPromptAsync("Nowa Kolekcja", "Podaj typ kolekcji (np. Książki, Gry, Karty TCG):");
            Collection newCollection = new Collection(name, type ?? "");
            collectionList.AddCollection(newCollection);
        }
    }

    // Opens edit page for selected collection (CollectionDetailPage) - allows managing items and properties
    private async void OnEditCollectionClicked(object sender, EventArgs e)
    {
        if (CollectionsCollectionView.SelectedItem is Collection collection)
        {
            selectedCollection = collection;
            await Navigation.PushAsync(new CollectionDetailPage(collection, collectionList));
        }
        else
        {
            await this.DisplayAlert("Błąd", "Wybierz kolekcję do edycji", "OK");
        }
    }

    // Deletes selected collection - asks for confirmation with collection name
    private async void OnDeleteCollectionClicked(object sender, EventArgs e)
    {
        if (CollectionsCollectionView.SelectedItem is Collection collection)
        {
            bool confirm = await this.DisplayAlert("Potwierdzenie", 
                $"Czy na pewno chcesz usunąć kolekcję \"{collection.Name}\"?", 
                "Tak", "Nie");
            if (confirm)
            {
                collectionList.RemoveCollection(collection);
            }
        }
        else
        {
            await this.DisplayAlert("Błąd", "Wybierz kolekcję do usunięcia", "OK");
        }
    }

    // Called when page appears - loads collections from file and refreshes CollectionView
    protected override void OnAppearing()
    {
        base.OnAppearing();
        collectionList.LoadCollections();
        CollectionsCollectionView.ItemsSource = collectionList.Collections;
    }

    // Opens collection summary page for selected collection - shows statistics (how many we have, want to sell, sold)
    private async void OnSummaryClicked(object sender, EventArgs e)
    {
        if (CollectionsCollectionView.SelectedItem is Collection collection)
        {
            await Navigation.PushAsync(new CollectionSummaryPage(collection));
        }
        else
        {
            await DisplayAlert("Błąd", "Wybierz kolekcję, aby zobaczyć podsumowanie", "OK");
        }
    }

    // Exports selected collection to text file in app cache directory
    private async void OnExportCollectionClicked(object sender, EventArgs e)
    {
        if (CollectionsCollectionView.SelectedItem is Collection collection)
        {
            try
            {
                string path = await collectionList.ExportCollectionAsync(collection);
                if (path != null)
                {
                    await DisplayAlert("Sukces", $"Kolekcja wyeksportowana:\n{path}", "OK");
                }
                else
                {
                    await DisplayAlert("Anulowano", "Eksport został anulowany", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Błąd podczas eksportu: {ex.Message}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Błąd", "Wybierz kolekcję do eksportu", "OK");
        }
    }

    // Imports collection from file - if type exists, merges items; if new type, creates new collection
    private async void OnImportCollectionClicked(object sender, EventArgs e)
    {
        try
        {
            bool success = await collectionList.ImportCollectionAsync();
            if (success)
            {
                await DisplayAlert("Sukces", "Kolekcja została zaimportowana", "OK");
                collectionList.LoadCollections();
                CollectionsCollectionView.ItemsSource = collectionList.Collections;
            }
            else
            {
                await DisplayAlert("Anulowano", "Import został anulowany", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Błąd podczas importu: {ex.Message}", "OK");
        }
    }
}