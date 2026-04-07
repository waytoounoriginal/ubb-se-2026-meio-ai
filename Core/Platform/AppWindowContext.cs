using ubb_se_2026_meio_ai.Core.Platform;

namespace ubb_se_2026_meio_ai.Core.Platform
{
    public sealed class AppWindowContext : IAppWindowContext
    {
        public nint GetMainWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        }

        public bool TryEnqueueOnUiThread(Action callback)
        {
            return App.MainWindow.DispatcherQueue.TryEnqueue(() => callback());
        }
    }
}
