namespace RepyPharma.Infrastructure.Identity;

public static class AuthRoles
{
    public const string Admin = "Admin";
    public const string Farmacia = "Farmacia";
    public const string Almoxarifado = "Almoxarifado";
    public const string Consulta = "Consulta";

    public static readonly string[] All =
    [
        Admin,
        Farmacia,
        Almoxarifado,
        Consulta
    ];
}
