using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

// Articol dintr-o comanda a clientului, pentru MyOrdersView.
public class ArticolComandaClientDto
{
    public string Denumire { get; set; } = string.Empty;
    public decimal Cantitate { get; set; }
    public decimal PretUnitar { get; set; }

    // Ex. "2 x Pizza Margherita", pentru binding direct in XAML.
    public string TextAfisare => $"{Cantitate:0.##} x {Denumire}";
}

// Comanda clientului curent cu toate articolele - gruparea randurilor din
// dbo.sp_GetComenziClientCuDetalii dupa ComandaId. Vezi IOrderService.GetComenziClientAsync.
public class ComandaClientDto
{
    public int ComandaId { get; set; }
    public string CodUnic { get; set; } = string.Empty;
    public DateTime DataComanda { get; set; }
    public string Stare { get; set; } = string.Empty;
    public List<ArticolComandaClientDto> Articole { get; set; } = new();
    public decimal CostTransport { get; set; }

    // Procentul de discount al comenzii (snapshot de la creare), 0-100.
    public decimal Discount { get; set; }

    public DateTime? OraEstimataLivrare { get; set; }

    // Suma articolelor, fara transport/discount.
    public decimal SubtotalMancare { get; set; }

    // SubtotalMancare - (SubtotalMancare * Discount / 100) + CostTransport.
    public decimal Total { get; set; }

    // False daca Stare e "livrata"/"anulata"; altfel poate fi urmarita/anulata.
    public bool EsteActiva { get; set; }

    public bool AreDiscount => Discount > 0;
}
