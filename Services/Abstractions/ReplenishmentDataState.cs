public class ReplenishmentDataState
{
    public event Action? OnChange;

    public void NotifyChanged()
    {
        OnChange?.Invoke();
    }
}
