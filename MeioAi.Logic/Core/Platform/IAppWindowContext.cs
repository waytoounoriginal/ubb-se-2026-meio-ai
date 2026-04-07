namespace ubb_se_2026_meio_ai.Core.Platform
{
    public interface IAppWindowContext
    {
        nint GetMainWindowHandle();

        bool TryEnqueueOnUiThread(Action callback);
    }
}
