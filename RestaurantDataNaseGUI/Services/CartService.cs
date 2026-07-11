using System;
using System.Collections.ObjectModel;
using System.Linq;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Implementare in-memory a ICartService. Ca SessionService, Instance e un
// singleton simplu folosit de ViewModels cat timp nu exista DI. Se goleste
// automat la logout (cosul nu trebuie sa supravietuiasca sesiunii).
public sealed class CartService : ICartService
{
    public static CartService Instance { get; } = new(SessionService.Instance);

    private readonly ISessionService _sessionService;

    public ObservableCollection<ArticolCosDto> Articole { get; } = new();

    public event EventHandler? CosSchimbat;

    public CartService(ISessionService? sessionService = null)
    {
        _sessionService = sessionService ?? SessionService.Instance;
        _sessionService.CurrentUserChanged += OnCurrentUserChanged;
    }

    public void AdaugaArticol(ArticolCosDto articol)
    {
        var existent = GasesteArticolExistent(articol.PreparatId, articol.MeniuId);
        if (existent is not null)
        {
            existent.Cantitate += articol.Cantitate;
            CosSchimbat?.Invoke(this, EventArgs.Empty);
            return;
        }

        Articole.Add(articol);
        CosSchimbat?.Invoke(this, EventArgs.Empty);
    }

    public void AdaugaInCos(MeniuAfisareDto item, decimal cantitate = 1m)
    {
        AdaugaArticol(new ArticolCosDto
        {
            PreparatId = item.EsteMeniuCompus ? null : item.Id,
            MeniuId = item.EsteMeniuCompus ? item.Id : null,
            Denumire = item.Denumire,
            PretUnitar = item.Pret,
            Cantitate = cantitate,
        });
    }

    public void ModificaCantitate(ArticolCosDto articol, decimal cantitateNoua)
    {
        if (cantitateNoua <= 0)
        {
            StergeArticol(articol);
            return;
        }

        articol.Cantitate = cantitateNoua;
        CosSchimbat?.Invoke(this, EventArgs.Empty);
    }

    public void StergeArticol(ArticolCosDto articol)
    {
        if (Articole.Remove(articol))
        {
            CosSchimbat?.Invoke(this, EventArgs.Empty);
        }
    }

    public void GolesteCos()
    {
        if (Articole.Count == 0)
        {
            return;
        }

        Articole.Clear();
        CosSchimbat?.Invoke(this, EventArgs.Empty);
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        if (!_sessionService.EsteAutentificat)
        {
            GolesteCos();
        }
    }

    private ArticolCosDto? GasesteArticolExistent(int? preparatId, int? meniuId)
    {
        return Articole.FirstOrDefault(a => a.PreparatId == preparatId && a.MeniuId == meniuId);
    }
}
