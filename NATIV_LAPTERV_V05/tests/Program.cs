using LaptervNative;
using System.Xml.Linq;

void Assert(bool ok,string description)
{
    if(!ok)throw new Exception("Teszt sikertelen: "+description);
    Console.WriteLine("OK: "+description);
}
var p=new ProjectData{BoardLength=2800,BoardWidth=2070,Trim=10,Kerf=5,Thickness=18,Material="Feya",MachineMaterialCode="BLAP003"};
p.Parts.Add(new PartSpec{Name="Polc",Width=400,Length=700,Quantity=5});
p.Parts.Add(new PartSpec{Name="Oldal",Width=600,Length=900,Quantity=2});
var plan=Planner.Build(p);
Assert(plan.TotalParts==7,"minden darab bekerult");
Assert(plan.Boards.Count>=1,"tábla létrejött");
Assert(plan.Operations.Select(op=>op.Phase).SkipWhile(phase=>phase=="Sávozás").All(phase=>phase=="Hosszvágás"),
    "minden sávozás a hosszvágások előtt");
for(int i=0;i<plan.Boards.Count;i++)
{
    var doc=XDocument.Parse(XprgExporter.BoardXml(plan,plan.Boards[i]));
    var cuts=doc.Descendants("Cut").ToArray();
    Assert(cuts.First().Attribute("T")?.Value=="P","P fejparancs");
    Assert(cuts.Count(c=>c.Attribute("T")?.Value=="Y")==plan.Boards[i].Strips.Count,"minden sávnak Y parancs");
    Assert(cuts.Count(c=>c.Attribute("T")?.Value=="X")==plan.Boards[i].Strips.Sum(s=>s.Pieces.Count),"minden darabhoz X parancs");
    Assert(cuts.All(c=>c.Attribute("Rip")?.Value=="1"),"csak egy réteg kerül exportálásra");
    Assert(cuts.Select((c,j)=>c.Attribute("ID")?.Value==j.ToString()).All(ok=>ok),"folyamatos parancs ID");
}
var names=plan.Boards.Select((b,i)=>$"proba_{i+1}.xPrg").ToList();
var mix=XDocument.Parse(XprgExporter.MixXml(plan,"proba",names));
Assert(mix.Descendants("RigaMix").Count()==plan.Boards.Count,"Mix minden táblához hivatkozás");
var err=new ProjectData{BoardLength=800,BoardWidth=500,Trim=10,Kerf=5};
err.Parts.Add(new PartSpec{Width=700,Length=1200,Quantity=1});
try{Planner.Build(err);throw new Exception("Nem dobott hibát a túlméretre");}
catch(ArgumentException){Console.WriteLine("OK: túlméretes darabot blokkolja");}

// 0.5: alternatív, teljes keresztmetszetű guillotine kiosztás.
var econ = Planner.BuildEconomy(p);
Planner.ValidateGeometry(econ);
Assert(econ.TotalParts == plan.TotalParts, "gazdaságos mód: darabszám megmarad");
Assert(econ.Boards.Count <= plan.Boards.Count, "gazdaságos mód nem kér több táblát");
Assert(econ.Operations.Select(op=>op.Phase).SkipWhile(phase=>phase=="Sávozás")
    .All(phase=>phase=="Hosszvágás"), "gazdaságos módban is előbb teljes sávozás");
Assert(econ.Boards.SelectMany(b=>b.Strips).All(s=>s.Pieces.All(x=>
    Math.Abs(x.Width-s.Width)<.0001 && Math.Abs(x.Y-s.Y)<.0001)),
    "minden hosszvágás a teljes különálló csíkot keresztezi");
Assert(econ.LargestRemnantArea >= 0 && econ.ReusableRemnantArea >= 0,
    "a felhasználható egybefüggő maradékok számszerűsítve");
var econXml=XDocument.Parse(XprgExporter.BoardXml(econ,econ.Boards[0]));
Assert(econXml.Descendants("Cut").Count(x=>x.Attribute("T")?.Value=="Y")==econ.Boards[0].Strips.Count,
    "az új mód kísérleti xPrg-je minden sávot tartalmaz");
// Reprodukálható, több szélességet és hosszt kombináló próbatervek.
var random = new Random(20261029);
for(int seed=0;seed<12;seed++)
{
    var q=new ProjectData { BoardLength=2500,BoardWidth=1700,Trim=10,Kerf=5,Thickness=18 };
    for(int j=0;j<20;j++)
        q.Parts.Add(new PartSpec { Name="Teszt-"+seed+"-"+j,
            Width=new[]{220,300,450,500}[random.Next(4)],
            Length=new[]{380,510,680,830,1100,1350}[random.Next(6)],
            Quantity=1+random.Next(3) });
    var oldPlan=Planner.BuildClassic(q);
    var newPlan=Planner.BuildEconomy(q);
    Planner.ValidateGeometry(oldPlan);
    Planner.ValidateGeometry(newPlan);
    Assert(newPlan.Boards.Count<=oldPlan.Boards.Count,
        "kevert méret teszt "+seed+": nem több tábla, nincs ütközés, darabszám stimmel");
}

Console.WriteLine("MINDKÉT VÁGÁSI MÓD ELLENŐRZÉSE SIKERES");
