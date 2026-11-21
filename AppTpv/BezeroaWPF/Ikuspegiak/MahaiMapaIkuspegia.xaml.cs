// ============================================================
//  AppTpv — Mahaien mapa (6. fitxa): 7 mahaien egoera
//  Datuak zerbitzaritik ekartzen dira (MESAS_LISTA):
//    librea / erreserbatuta / okupatuta + eskaera irekiaren zenbakia.
//  Mahi batean klik egitean erreserba-formularioa irekitzen da.
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Ikuspegiak;

public partial class MahaiMapaIkuspegia : UserControl
{
    private MahaiDatuak? _hautatutakoMahia;

    public MahaiMapaIkuspegia()
    {
        InitializeComponent();
        Loaded += (_, _) => Kargatu();
    }

    private void BtnFreskatu_Click(object sender, RoutedEventArgs e)
    {
        Kargatu();
        Eguneratu();
    }

    private void Kargatu()
    {
        var r = Konexioa.Bidali("MESAS_LISTA");
        if (!r.Arrakasta)
        {
            TxtMapaInfo.Text = r.Errorea;
            return;
        }

        var mahaiak = JsonSerializer.Deserialize<List<MahaiDatuak>>(
            r.Datuak.GetProperty("mahaiak").GetRawText()) ?? new();
        ListaMahaiak.ItemsSource = mahaiak;

        var okupatuta = mahaiak.Count(m => m.Egoera == "okupatuta");
        var erreserbatuta = mahaiak.Count(m => m.Egoera == "erreserbatuta");
        TxtMapaInfo.Text = $"{mahaiak.Count} mahai · okupatuta: {okupatuta} · erreserbatuta: {erreserbatuta}";
    }

    private void Eguneratu()
    {
        var iragazkia = new { data = TxtDataIragazkia.Text.Trim() };
        var r = Konexioa.Bidali("RESERVA_LISTA", iragazkia);
        TxtErreserbaAbisua.Text = r.Arrakasta ? "" : r.Errorea;
        var erreserbak = r.Arrakasta
            ? JsonSerializer.Deserialize<List<ErreserbaDatuak>>(
                r.Datuak.GetProperty("erreserbak").GetRawText()) ?? new()
            : new List<ErreserbaDatuak>();
        GridErreserbak.ItemsSource = erreserbak;
    }

    private void BtnEguneratu_Click(object sender, RoutedEventArgs e) => Eguneratu();

    // Mahai-txartela sakatzean: formularioa ireki eta mahiaren zenbakia jarri
    private void MahaiKlik(object sender, MouseButtonEventArgs e)
    {
        if ((sender as Border)?.DataContext is not MahaiDatuak mahia) return;

        _hautatutakoMahia = mahia;
        TxtFormaIzenburua.Text = $"Erreserba berria — {mahia.MahaiZenbakia}. mahaia";
        TxtMahia.Text = $"{mahia.MahaiZenbakia}. mahaia ({mahia.Edukiera} plaza)";
        CboTxanda.SelectedIndex = 0;
        TxtPertsonak.Text = Math.Min(2, mahia.Edukiera).ToString();
        TxtData.Text = "";
        TxtBezeroa.Text = "";
        TxtOharrak.Text = "";
        TxtAbisua.Text = "";

        MapaPanel.Visibility = Visibility.Collapsed;
        FormaPanel.Visibility = Visibility.Visible;
    }

    private void TxtPertsonak_TextChanged(object sender, TextChangedEventArgs e)
    {
        // XAML-a hasieratzen ari den bitartean TxtAbisua oraindik ez dago sortuta
        if (TxtAbisua == null || _hautatutakoMahia == null) return;

        if (int.TryParse(TxtPertsonak.Text.Trim(), out var kopurua) &&
            kopurua > _hautatutakoMahia.Edukiera)
        {
            TxtAbisua.Text = $"Ez zarete {kopurua} sartzen";
        }
        else if (TxtAbisua.Text.StartsWith("Ez zarete"))
        {
            TxtAbisua.Text = "";
        }
    }

    private void BtnSortu_Click(object sender, RoutedEventArgs e)
    {
        if (_hautatutakoMahia == null)
        {
            TxtAbisua.Text = "Aukeratu mahia.";
            return;
        }

        var txanda = (CboTxanda.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        int.TryParse(TxtPertsonak.Text.Trim(), out var pertsonak);
        var data = TxtData.Text.Trim();
        var bezeroa = TxtBezeroa.Text.Trim();
        var oharrak = TxtOharrak.Text.Trim();

        if (txanda == "" || pertsonak <= 0 || bezeroa == "")
        {
            TxtAbisua.Text = "Bete txanda, pertsona kopurua (> 0) eta bezeroaren izena.";
            return;
        }
        // Edukiera gainditzen bada ezin da erreserba sortu
        if (pertsonak > _hautatutakoMahia.Edukiera)
        {
            TxtAbisua.Text = $"Ez zarete {pertsonak} sartzen";
            return;
        }

        var r = Konexioa.Bidali("RESERVA_CREAR", new
        {
            mahaiId = _hautatutakoMahia.Id,
            txanda,
            pertsonaKopurua = pertsonak,
            data,
            bezeroIzena = bezeroa,
            oharrak
        });
        if (!r.Arrakasta)
        {
            TxtAbisua.Text = r.Errorea;
            return;
        }

        // Mapara bueltatu eta mahaien zein erreserben datuak freskatu
        FormaItxi();
        Kargatu();
        Eguneratu();
    }

    private void BtnUtzi_Click(object sender, RoutedEventArgs e) => FormaItxi();

    private void FormaItxi()
    {
        _hautatutakoMahia = null;
        FormaPanel.Visibility = Visibility.Collapsed;
        MapaPanel.Visibility = Visibility.Visible;
    }
}
