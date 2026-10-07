namespace Stars.Client;

public partial class QualityPage : ContentPage
{
	public QualityPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

}
