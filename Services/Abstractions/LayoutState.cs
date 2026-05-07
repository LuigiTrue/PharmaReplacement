public class LayoutState
{
    public bool IsMenuCollapsed { get; private set; }

    public event Action? OnChange;

    public void Toggle()
    {
        IsMenuCollapsed = !IsMenuCollapsed;
        NotifyStateChanged();
    }

    public void Collapse()
    {
        IsMenuCollapsed = true;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}