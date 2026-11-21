// ============================================================
//  AppTpv — datuetarako sarbide-geruza (SQLite)
//
//  Erronkaren arauak hemen ebazten dira:
//   * Dena BDaren aurka egiten da (tpv.db fitxategia).
//   * IDAZKETAK lock global bakar batez serializatzen dira (enuntziatuak
//     baimentzen du eta hondatzeak saihesten ditu).
//   * BD bakarrik sortzen da lehen abiaraztean, Datuak/Eskema.sql-etik.
// ============================================================
using Microsoft.Data.Sqlite;

namespace ZerbitzariaTPV.Datuak;

public static class DatuBasea
{
    // Fitxategi-bidea: data/ karpeta, exekutagarriaren ondoan
    public static string BDRuta { get; } =
        Path.Combine(AppContext.BaseDirectory, "data", "tpv.db");

    // IDAZKETEN LOCK GLOBALA
    // aldaketa guztiak lock honetatik pasatzen dira; irakurketak lock gabe (WAL).
    public static readonly object IdazketaBlokeoa = new();

    public static SqliteConnection Konektatu()
    {
        return new SqliteConnection($"Data Source={BDRuta}");
    }

    /// <summary>BD sortu eta eskema + datuak hasieratzen ditu, lehendik ez bazegoen.</summary>
    public static void Hasi()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BDRuta)!);

        var berria = !File.Exists(BDRuta) || new FileInfo(BDRuta).Length == 0;

        using var konexioa = Konektatu();
        konexioa.Open();

        // MIGRAZIOA: lehendik dauden datu-baseetan Pedido.idMesa zutabea gehitu.
        
        using (var cmd = konexioa.CreateCommand())
        {
            // sqlite_master kontsultatzen da Pedido taula existitzen den egiaztatzeko.
            cmd.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Pedido'";
            // ExecuteScalar-ek lehen emaitza itzultzen du: 1 badago, 0 bestela.
            var taulaBadago = Convert.ToInt32(cmd.ExecuteScalar()) > 0;

            if (taulaBadago)
            {
                // pragma_table_info-k Pedido taularen zutabeak itzultzen ditu.
                cmd.CommandText =
                    "SELECT COUNT(*) FROM pragma_table_info('Pedido') WHERE name = 'idMesa'";
                // idMahaia zutabea badagoen egiaztatzen du. 
                var IdMahaiaBadago = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                if (!IdMahaiaBadago)
                {
                    // Zutabea faltan badago, taulari gehitzen zaio.
                    cmd.CommandText =
                        "ALTER TABLE Pedido ADD COLUMN idMesa INTEGER REFERENCES Mesa(id)";
                    // ALTER TABLE exekutatzen da; ExecuteNonQuery errenkada-kontsultarik gabea da.
                    cmd.ExecuteNonQuery();
                    // Terminalean migrazioa gauzatu dela adierazten du.
                    Console.WriteLine("[BD] Migrazioa: Pedido.idMesa gehitu da");
                }
            }
        }

        // Eskema exekutatzen da (idempotentea: dena IF NOT EXISTS)
        var esquema = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Datuak", "Eskema.sql"));
        using (var cmd = konexioa.CreateCommand())
        {
            cmd.CommandText = esquema;
            cmd.ExecuteNonQuery();
        }

        if (berria)
            ErabiltzaileakSortu(konexioa);
    }

    private static void ErabiltzaileakSortu(SqliteConnection konexioa)
    {
        // Hasierako administratzailea (entregable: kredentzialak dokumentatuta)
        // pasahitza "admin123" -> fitxategiko sal konstantearekin SHA-256
        using (var cmd = konexioa.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Usuario (login, passHash, nombre, rol)
                VALUES ('admin', @hash, 'Administrador', 'admin');";
            cmd.Parameters.AddWithValue("@hash",
                Segurtasuna.PasahitzaHash("admin123"));
            cmd.ExecuteNonQuery();
        }

        // 15 erabiltzaile adibide: pasahitza "1234", rola 'erabiltzailea'
        // (UIak "Erabiltzailea" erakusten du rol horrentzat)
        string[] erabiltzaileak = {
            "ane.azkue|Ane Azkue",
            "iker.etxeberria|Iker Etxeberria",
            "nerea.garai|Nerea Garai",
            "jon.aranburu|Jon Aranburu",
            "maddi.oiartzabal|Maddi Oiartzabal",
            "gorka.larralde|Gorka Larralde",
            "uxue.mendoza|Uxue Mendoza",
            "asier.bengoa|Asier Bengoa",
            "leire.sarobe|Leire Sarobe",
            "eneko.iturbe|Eneko Iturbe",
            "amets.salazar|Amets Salazar",
            "ihintza.barandiaran|Ihintza Barandiaran",
            "hodei.mintegi|Hodei Mintegi",
            "eider.zabaleta|Eider Zabaleta",
            "unai.berasategi|Unai Berasategi"
        };
        using (var cmd = konexioa.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Usuario (login, passHash, nombre, rol)
                VALUES (@login, @hash, @nombre, 'usuario');";
            var pLogin  = cmd.Parameters.Add("@login",  SqliteType.Text);
            var pHash   = cmd.Parameters.Add("@hash",   SqliteType.Text);
            var pIzena = cmd.Parameters.Add("@nombre", SqliteType.Text);
            pHash.Value = Segurtasuna.PasahitzaHash("1234");
            foreach (var erabiltzailea in erabiltzaileak)
            {
                var zatiak = erabiltzailea.Split('|');
                pLogin.Value  = zatiak[0];
                pIzena.Value  = zatiak[1];
                cmd.ExecuteNonQuery();
            }
        }

        // 7 mahai: 4 laguneko 2, 6 laguneko 2 eta 10 laguneko 3
        using (var cmd = konexioa.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (1, 4);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (2, 4);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (3, 6);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (4, 6);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (5, 10);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (6, 10);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (7, 10);";
            cmd.ExecuteNonQuery();
        }

        // Produktuak IZAKINEKIN (biltegiaren eskakizuna)
        using (var cmd = konexioa.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Producto (nombre, precio, stock, ingredientes, categoria) VALUES
                ('Kola kontxa',   2.5, 24, 'kola, haragi eskuragabea', 'edariak'),
                ('Kalimotxo',     3.0, 40, 'ardo gorria, pilare',      'edariak'),
                ('Prentxikoa',    3.0, 12, 'tiegno, flan',             'edariak'),
                ('Sakramento burger', 6.5, 8, 'ogia, haragitxoa, gazta atxurra', 'janari'),

                ('Hutsa',      1.70, 20, '', 'Kafeak'),
                ('Ebakia',     1.75, 20, '', 'Kafeak'),
                ('kafesnea',   1.80, 20, '', 'Kafeak'),

                ('koka-kola',  2.50, 20, '', 'Freskagarriak'),
                ('Limoia',     2.50, 20, '', 'Freskagarriak'),
                ('laranja',    2.50, 20, '', 'Freskagarriak'),
                ('Tonika',     2.50, 20, '', 'Freskagarriak'),
                ('Nestea',     2.50, 20, '', 'Freskagarriak'),
                ('Aquarius',   2.50, 20, '', 'Freskagarriak'),

                ('Zuritoa',    1.80, 20, '', 'Garagardoa'),
                ('Kaña',       3.00, 20, '', 'Garagardoa'),
                ('Botila',     2.50, 20, '', 'Garagardoa'),

                ('Urtekoa',    2.00, 20, '', 'Ardo Beltzak'),
                ('kriantzak',  3.00, 20, '', 'Ardo Beltzak'),

                ('Rueda',      2.00, 20, '', 'Ardo Txuriak'),
                ('Berdejo',    2.50, 20, '', 'Ardo Txuriak'),
                ('chardonay',  3.00, 20, '', 'Ardo Txuriak');";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Programa.cs-ek erabiltzen duen osasun-proba txikia.</summary>
    public static (string Resumen, string Konexioa) Egiaztatu()
    {
        using var konexioa = Konektatu();
        konexioa.Open();
        using var cmd = konexioa.CreateCommand();
        cmd.CommandText = @"
            SELECT (SELECT COUNT(*) FROM Usuario) AS usuarios,
                   (SELECT COUNT(*) FROM Mesa)    AS mesas,
                   (SELECT COUNT(*) FROM Producto) AS productos;";
        using var irakurlea = cmd.ExecuteReader();
        irakurlea.Read();
        return ($"erabiltzaileak={irakurlea.GetInt32(0)} mahaiak={irakurlea.GetInt32(1)} produktuak={irakurlea.GetInt32(2)}",
                konexioa.ConnectionString);
    }
}
