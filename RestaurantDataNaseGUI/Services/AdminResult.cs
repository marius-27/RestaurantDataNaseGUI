namespace RestaurantDataNaseGUI.Services;

/// <summary>Rezultatul unei operatii de administrare (Create/Update/Delete) din IAdminService.</summary>
public sealed class AdminResult
{
    public bool Succes { get; }
    public string? MesajEroare { get; }

    private AdminResult(bool succes, string? mesajEroare)
    {
        Succes = succes;
        MesajEroare = mesajEroare;
    }

    public static AdminResult Ok() => new(true, null);

    public static AdminResult Esec(string mesajEroare) => new(false, mesajEroare);
}
