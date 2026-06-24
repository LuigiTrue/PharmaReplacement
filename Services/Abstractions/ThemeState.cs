public class ThemeState
{
    public bool IsDarkMode { get; private set; } = true;

    public event Action? OnChange;

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
