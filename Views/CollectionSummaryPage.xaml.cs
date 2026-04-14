namespace Collection_Management;

using Collection_Management.Models;

public partial class CollectionSummaryPage : ContentPage
{
    private Collection collection;

    public CollectionSummaryPage(Collection collection)
    {
        InitializeComponent();
        this.collection = collection;
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        // Set header information
        CollectionNameLabel.Text = collection.Name;
        CollectionTypeLabel.Text = $"Typ: {collection.Type}";

        // Calculate statistics
        int totalItems = collection.Items.Count;
        int possessCount = collection.Items.Count(i => i.Status == ItemStatus.Possess);
        int wantToSellCount = collection.Items.Count(i => i.Status == ItemStatus.WantToSell);
        int soldCount = collection.Items.Count(i => i.Status == ItemStatus.Sold);

        // Update labels
        TotalItemsLabel.Text = totalItems.ToString();
        PossessLabel.Text = possessCount.ToString();
        WantToSellLabel.Text = wantToSellCount.ToString();
        SoldLabel.Text = soldCount.ToString();

        // Calculate and display percentages
        if (totalItems > 0)
        {
            double possessPercentage = (double)possessCount / totalItems * 100;
            double wantToSellPercentage = (double)wantToSellCount / totalItems * 100;
            double soldPercentage = (double)soldCount / totalItems * 100;

            PossessPercentageLabel.Text = $"{possessPercentage:F1}% z łącznej liczby";
            WantToSellPercentageLabel.Text = $"{wantToSellPercentage:F1}% z łącznej liczby";
            SoldPercentageLabel.Text = $"{soldPercentage:F1}% z łącznej liczby";
        }
        else
        {
            PossessPercentageLabel.Text = "0% z łącznej liczby";
            WantToSellPercentageLabel.Text = "0% z łącznej liczby";
            SoldPercentageLabel.Text = "0% z łącznej liczby";
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
