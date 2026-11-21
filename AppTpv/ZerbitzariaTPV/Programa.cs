// ============================================================
//  ---------ZerbitzariaTPV —---
//  BD abiarazi eta bezeroei 5001 atakan erantzuten die.
//  Idatzi "parar" + Enter garbi ixteko (Ctrl+C ere balio du).
// ============================================================
using ZerbitzariaTPV.Datuak;
using ZerbitzariaTPV.Sarea;

Console.WriteLine("=== ZerbitzariaTPV ===");

DatuBasea.Hasi();
var (laburpena, _) = DatuBasea.Egiaztatu();
Console.WriteLine($"BD OK -> {laburpena}");

var zerbitzaria = new Thread(TCPZerbitzaria.ZerbitzariHaria.Abiarazi)
{ IsBackground = true };
zerbitzaria.Start();

Console.WriteLine("Zerbitzaria martxan. Idatzi 'ITZALI' ixteko.");
while (true)
{
    var lerroa = Console.ReadLine();
    if (lerroa != null && lerroa.Trim().ToLower() == "ITZALI")
    {
        Console.WriteLine("Zerbitzaria ixten...");
        Environment.Exit(0);
    }
}
