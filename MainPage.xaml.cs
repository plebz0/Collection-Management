namespace Collection_Management;

using Collection_Management.Models;


public partial class MainPage : ContentPage
{
    private CollectionList collectionList;
    private Collection selectedCollection;

    public MainPage()
    {
        InitializeComponent();
        collectionList = new CollectionList();
        CollectionsCollectionView.ItemsSource = collectionList.Collections;
    }

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        collectionList.LoadCollections();
        CollectionsCollectionView.ItemsSource = collectionList.Collections;
    }
}
