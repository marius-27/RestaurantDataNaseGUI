using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>Un articol dintr-o comanda a clientului, pentru afisare in MyOrdersView.</summary>
public class ArticolComandaClientDto
{
    public string Denumire { get; set; } = string.Empty;
    public decimal Cantitate { get; set; }
    public decimal PretUnitar { get; set; }

    /// <summary>"2 x Pizza Margherita" - pentru binding direct in XAML fara StringFormat compus.</summary>
    public string TextAfisare => $"{Cantitate:0.##} x {Denumire}";
}

/// <summary>
/// O comanda a clientului curent, cu toate articolele ei - rezultatul
/// gruparii randurilor din dbo.sp_GetComenziClientCuDetalii (un rand per
/// articol) dupa ComandaId. Vezi IOrderService.GetComenziClientAsync.
/// </summary>
public class ComandaClientDto
{
    public int ComandaId { get; set; }
    public string CodUnic { get; set; } = string.Empty;
    public DateTime DataComanda { get; set; }
    public string Stare { get; set; } = string.Empty;
    public List<ArticolComandaClientDto> Articole { get; set; } = new();
    public decimal CostTransport { get; set; }

    /// <summary>Procentul de discount aplicat la aceasta comanda (snapshot de la creare), 0-100.</summary>
    public decimal Discount { get; set; }

    public DateTime? OraEstimataLivrare { get; set; }

    /// <summary>Suma articolelor (Cantitate * PretUnitar), fara transport/discount.</summary>
    public decimal SubtotalMancare { get; set; }

    /// <summary>SubtotalMancare - (SubtotalMancare * Discount / 100) + CostTransport.</summary>
    public decimal Total { get; set; }

    /// <summary>False daca Stare e "livrata" sau "anulata" - poate fi inca urmarita/anulata cat timp e true.</summary>
    public bool EsteActiva { get; set; }

    public bool AreDiscount => Discount > 0;
}
