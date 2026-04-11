namespace ubb_se_2026_meio_ai.Core.Services
{
    /// <summary>
    /// Abstraction for page navigation so ViewModels can navigate
    /// without coupling to a WinUI Frame.
    /// </summary>
    public interface INavigationService
    {
        void NavigateTo<TPage>()
            where TPage : class;
        void NavigateTo(Type pageType);
        void GoBack();
        bool CanGoBack { get; }
    }
}
