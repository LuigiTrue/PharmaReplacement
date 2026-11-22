public class StockDataState
{
    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();
}
