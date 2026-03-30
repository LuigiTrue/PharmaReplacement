public enum ReplenishmentPriority
{
    Critical,  // Estoque abaixo do mínimo
    Warning,   // Estoque próximo do mínimo (ex: até 20% acima)
    Ok         // Estoque adequado
}