using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
namespace LaptervNative;
public static class XprgExporter
{
    private static readonly CultureInfo C=CultureInfo.InvariantCulture;
    private static string N(double n)=>n.ToString("0.00",C);
    private static string Esc(string? s)=>System.Security.SecurityElement.Escape(s??"")??"";
    private static void Cut(StringBuilder xml,string t,double dim,int id)
    {
        xml.Append("\t\t<Cut T=\"").Append(t).Append("\" Dim=\"").Append(N(dim))
            .Append("\" Rip=\"1\" ID=\"").Append(id).Append("\" />\r\n");
    }
    // This is a deliberately restricted DRAFT writer: P, RY, Y, RX, X, F only.
    // Do not infer U/V/stacking commands. Test in the machine's offline simulator first.
    public static string BoardXml(CuttingPlan plan,BoardPlan board)
    {
        Planner.ValidateGeometry(plan);
        var p=plan.Project;
        if(string.IsNullOrWhiteSpace(p.MachineMaterialCode))throw new ArgumentException("A gépi anyagkód nem lehet üres.");
        var xml=new StringBuilder();
        xml.Append("<SchemaDoc>\r\n");
        xml.Append("\t<Header NumFogliPacco=\"1.00\" Descrizione=\"")
            .Append(Esc(p.MachineMaterialCode)).Append("\" Spessore=\"").Append(N(p.Thickness))
            .Append("\" Date=\"").Append(DateTime.Now.ToString("dd/MM/yyyy",C))
            .Append("\" Lunghezza=\"").Append(N(p.BoardLength))
            .Append("\" Larghezza=\"").Append(N(p.BoardWidth))
            .Append("\" SpessoreLama=\"").Append(N(p.Kerf)).Append("\" />\r\n");
        xml.Append("\t<Material Tipologia=\"\" Codice=\"").Append(Esc(p.MachineMaterialCode))
            .Append("\" />\r\n\t<Pattern>\r\n");
        int id=0;
        Cut(xml,"P",p.BoardLength,id++);
        Cut(xml,"RY",p.Trim,id++);
        foreach(var strip in board.Strips)
        {
            Cut(xml,"Y",strip.Width,id++);
            Cut(xml,"RX",p.Trim,id++);
            foreach(var piece in strip.Pieces)
            {
                Cut(xml,"X",piece.Length,id++);
                Cut(xml,"F",0,id++);
            }
        }
        xml.Append("\t</Pattern>\r\n</SchemaDoc>\r\n");
        string raw=xml.ToString();
        // Reject malformed outputs rather than silently supplying garbage to the saw.
        var root=XDocument.Parse(raw);
        var cuts=root.Root?.Element("Pattern")?.Elements("Cut").ToList()
            ?? throw new InvalidDataException("Pattern hiányzik");
        if(cuts.Count==0||cuts[0].Attribute("T")?.Value!="P" ||
           cuts.Select((node,i)=>node.Attribute("ID")?.Value==i.ToString(C)).Any(ok=>!ok))
            throw new InvalidDataException("XPrg szerkezeti ellenőrzés sikertelen");
        return raw;
    }
    public static string MixXml(CuttingPlan plan,string rootName,IReadOnlyList<string> names)
    {
        if(names.Count!=plan.Boards.Count)throw new InvalidOperationException("Programfájlok száma nem egyezik");
        var p=plan.Project;
        var s=new StringBuilder();
        s.Append("<xMix>\r\n\t<Header Descrizione=\"").Append(Esc(p.Furniture))
            .Append("\" Import=\"\" Date=\"").Append(DateTime.Now.ToString("dd/MM/yyyy",C))
            .Append("\" />\r\n\t<Materials>\r\n\t\t<Material Tipologia=\"")
            .Append(Esc(p.MachineMaterialCode)).Append("\" Codice=\"").Append(Esc(p.MachineMaterialCode))
            .Append("\" Quantita=\"999\" />\r\n\t</Materials>\r\n\t<RigheMix>\r\n");
        foreach(var file in names)
            s.Append("\t\t<RigaMix File=\".\\xPrg\\").Append(Esc(file))
             .Append("\" Rep=\"1\" Pacco=\"1\" CambioMateriale=\"0\" />\r\n");
        s.Append("\t</RigheMix>\r\n</xMix>\r\n");
        XDocument.Parse(s.ToString());
        return s.ToString();
    }
    public static string Export(CuttingPlan plan,string saveMixPath)
    {
        string dest=Path.GetFullPath(saveMixPath),folder=Path.GetDirectoryName(dest)
            ?? throw new InvalidOperationException("Hibás célmappa");
        string name=Path.GetFileNameWithoutExtension(dest);
        if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)
            throw new InvalidOperationException("Érvénytelen programnév");
        Directory.CreateDirectory(Path.Combine(folder,"xPrg"));
        var names=new List<string>();
        foreach(var board in plan.Boards)
        {
            string file=$"{name}_{board.Number}.xPrg";
            names.Add(file);
            File.WriteAllText(Path.Combine(folder,"xPrg",file),BoardXml(plan,board),new UTF8Encoding(false));
        }
        File.WriteAllText(dest,MixXml(plan,name,names),new UTF8Encoding(false));
        var sb=new StringBuilder();
        sb.AppendLine("LAPTERV 0.5 – KÍSÉRLETI GÉPI EXPORT").AppendLine()
          .AppendLine("NEM HITLESÍTETT FŰRÉSZGÉP PROGRAM!")
          .AppendLine("A mintához hasonló XML-fájl készült, de a vezérlés fizikai végrehajtása nincs igazolva.")
          .AppendLine("NEM SZABAD felügyelet nélküli vagy éles gyártásban használni.")
          .AppendLine("Vágás előtt: gyári offline szimulátor, táblaorientáció, origó, szélezés,")
          .AppendLine("ütköző, fűrészrés, gép típusa, teríték és műveletsorrend ellenőrzése.")
          .AppendLine("Többrétegű vágás parancsot NEM ad ki; Pacco=1, Rip=1.")
          .AppendLine("A csíkok csoportosítása csak előkészítési javaslat.")
          .AppendLine($"Kiválasztott kiosztás: {plan.ModeName}")
          .AppendLine($"Tábla: {plan.Project.BoardLength} × {plan.Project.BoardWidth}; darabok: {plan.TotalParts}; táblák: {plan.Boards.Count}.");
        File.WriteAllText(Path.Combine(folder,name+"_OLVASD_EL.txt"),sb.ToString(),Encoding.UTF8);
        return $"{plan.Boards.Count} db xPrg és 1 db xMix elkészült a(z) {folder} mappában.\n\n"+
          "Figyelem: ez még NEM hitelesített gépi program!";
    }
}
