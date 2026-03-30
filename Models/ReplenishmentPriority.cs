public enum ReplenishmentPriority
{
    Critical,  // Estoque abaixo do mínimo
    Warning,   // Estoque próximo do mínimo (ex: até 20% acima)
    Ok         // Estoque adequado
}

public enum ItemPriority
{
    UltraHigh = 0,
    High = 1,
    Moderate = 2,
    Low = 3       // Estoque adequado
}