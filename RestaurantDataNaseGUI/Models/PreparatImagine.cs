namespace RestaurantDataNaseGUI.Models;

public class PreparatImagine
{
    public int Id { get; set; }
    public int PreparatId { get; set; }
    public string CalePoza { get; set; } = string.Empty;

    public Preparat Preparat { get; set; } = null!;
}
