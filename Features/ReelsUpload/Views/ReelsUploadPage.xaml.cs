using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Views
{
    /// <summary>
    /// Code-behind for the Reels Upload page.
    /// Owner: Alex
    /// </summary>
    public sealed partial class ReelsUploadPage : Page
    {
        public ReelsUploadViewModel ViewModel { get; }

        public ReelsUploadPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsUploadViewModel>();
            this.InitializeComponent();
        }

        private void MovieAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only trigger search if user typed it (not if we programmatically populated it)
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.SearchMovieCommand.Execute(sender.Text);
            }
        }

        private void MovieAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is MovieCardModel pickedMovie)
            {
                // Update the text box to perfectly match the chosen name
                sender.Text = pickedMovie.Title;
                ViewModel.SelectMovieCommand.Execute(pickedMovie);
            }
        }
    }
}
