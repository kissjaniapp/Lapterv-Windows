using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.Json;
namespace LaptervNative;
public sealed class MainForm : Form
{
    private readonly TabControl _tabs = new() { Dock=DockStyle.Fill };
    private readonly DataGridView _parts = new() { Dock=DockStyle.Fill,BackgroundColor=Color.White,
        RowHeadersVisible=false,AllowUserToAddRows=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView _ops = new() { Dock=DockStyle.Fill,ReadOnly=true,BackgroundColor=Color.White,
        RowHeadersVisible=false,AllowUserToAddRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox _furniture = new(){Width=200};
    private readonly TextBox _destination = new(){Width=200};
    private readonly TextBox _material = new(){Width=125,Text="Feya"};
    private readonly TextBox _machineCode = new(){Width=115};
    private readonly Dictionary<string,NumericUpDown> _n = new();
    private readonly Label _status = new(){Dock=DockStyle.Bottom,Height=31,Text="Kész a tervezésre.",TextAlign=ContentAlignment.MiddleLeft,
        ForeColor=Color.FromArgb(47,64,88),Padding=new Padding(12,0,0,0)};
    private readonly Label _summary = new(){Dock=DockStyle.Top,Height=52,
        Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(12,10,0,0)};
    private readonly NumericUpDown _boardNumber=new(){Minimum=1,Maximum=1,Width=75};
    private readonly Panel _canvas=new(){Dock=DockStyle.Fill,BackColor=Color.White};
    private CuttingPlan? _plan;
    private CuttingPlan? _classicPlan;
    private CuttingPlan? _economyPlan;
    private readonly ComboBox _strategy = new() { Width = 340, DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly Label _comparison = new() { Dock=DockStyle.Top, Height=61,
        BackColor=Color.FromArgb(233,244,239), ForeColor=Color.FromArgb(35,76,65),
        Padding=new Padding(14,7,12,3),Font=new Font("Segoe UI",9.5f) };
    public MainForm()
    {
        Text="Lapterv – natív Windows szabásztervező 0.5 (két vágási mód)";
        MinimumSize=new Size(980,680);Size=new Size(1220,830);
        StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(245,247,251);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=54,WrapContents=false,
            BackColor=Color.FromArgb(24,42,65),Padding=new Padding(10,7,10,4)};
        toolbar.Controls.Add(Action("+ Alkatrész",(_,_)=>AddPart(),Color.FromArgb(20,166,132)));
        toolbar.Controls.Add(Action("Szabásterv készítése",(_,_)=>BuildPlan(),Color.FromArgb(57,125,215)));
        toolbar.Controls.Add(Action("Projekt mentése",(_,_)=>SaveProject(),Color.FromArgb(76,90,112)));
        toolbar.Controls.Add(Action("Projekt megnyitása",(_,_)=>OpenProject(),Color.FromArgb(76,90,112)));
        toolbar.Controls.Add(Action("Kísérleti .xPrg / .xMix",(_,_)=>ExportPlan(),Color.FromArgb(173,85,43)));
        Controls.Add(_tabs);Controls.Add(_status);Controls.Add(toolbar);
        InitDataTab();InitPreviewTab();InitMachineTab();
        _canvas.Paint+=PaintBoard;
    }
    private static Button Action(string caption,EventHandler click,Color c)
    {
        var b=new Button{Text=caption,AutoSize=true,Height=33,Margin=new Padding(3,2,7,2),
            FlatStyle=FlatStyle.Flat,BackColor=c,ForeColor=Color.White,Cursor=Cursors.Hand};
        b.FlatAppearance.BorderSize=0;b.Click+=click;return b;
    }
    private static Label Tag(string s)=>new(){Text=s,AutoSize=true,Padding=new Padding(0,8,0,0),Margin=new Padding(8,3,1,1)};
    private NumericUpDown Num(string key,decimal initial,int max=10000,int min=0)
    {
        var n=new NumericUpDown{Minimum=min,Maximum=max,Value=initial,DecimalPlaces=1,Increment=1,
            Width=90,Margin=new Padding(0,4,12,3),ThousandsSeparator=false};
        _n[key]=n;return n;
    }
    private void InitDataTab()
    {
        var tab=new TabPage("01  Bútor és alkatrészek"){BackColor=Color.FromArgb(245,247,251)};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,148));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var fields=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,AutoScroll=true,Padding=new Padding(8,8,8,0)};
        fields.Controls.Add(Tag("Bútor megnevezése:"));fields.Controls.Add(_furniture);
        fields.Controls.Add(Tag("Hová készül:"));fields.Controls.Add(_destination);
        fields.Controls.Add(Tag("Anyag színe:"));fields.Controls.Add(_material);
        fields.Controls.Add(Tag("Gépi anyagkód:"));fields.Controls.Add(_machineCode);
        fields.Controls.Add(Tag("Bútor szélesség:"));fields.Controls.Add(Num("fw",1000));
        fields.Controls.Add(Tag("Magasság:"));fields.Controls.Add(Num("fh",910));
        fields.Controls.Add(Tag("Mélység:"));fields.Controls.Add(Num("fd",600));
        var sheet=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,AutoScroll=true,Padding=new Padding(8,6,0,0),
            BackColor=Color.FromArgb(229,236,243)};
        sheet.Controls.Add(Tag("Alaptábla hossz:"));sheet.Controls.Add(Num("length",2800));
        sheet.Controls.Add(Tag("Szélesség:"));sheet.Controls.Add(Num("width",2070));
        sheet.Controls.Add(Tag("Vastagság:"));sheet.Controls.Add(Num("thick",18,100,1));
        sheet.Controls.Add(Tag("Fűrészrés:"));sheet.Controls.Add(Num("kerf",5,20,1));
        sheet.Controls.Add(Tag("Szélezés oldalanként:"));sheet.Controls.Add(Num("trim",10,100));
        foreach(Control c in new Control[]{_furniture,_destination,_material,_machineCode})c.Margin=new Padding(1,4,12,3);
        _parts.Columns.Add(new DataGridViewTextBoxColumn{Name="Part",HeaderText="Alkatrész megnevezése",FillWeight=170});
        _parts.Columns.Add(new DataGridViewTextBoxColumn{Name="Width",HeaderText="Sávszélesség (mm)"});
        _parts.Columns.Add(new DataGridViewTextBoxColumn{Name="Length",HeaderText="Hossz (mm)"});
        _parts.Columns.Add(new DataGridViewTextBoxColumn{Name="Qty",HeaderText="Darab" ,FillWeight=55});
        _parts.Columns.Add(new DataGridViewTextBoxColumn{Name="Edge",HeaderText="Élzárás",FillWeight=70});
        var gbox=new GroupBox{Text="Alkatrészek – képlet nem kötelező, csak írd be a kész méreteket",Dock=DockStyle.Fill};
        var pane=new Panel{Dock=DockStyle.Fill};
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=41};
        footer.Controls.Add(Action("+ Új alkatrész",(_,_)=>AddPart(),Color.FromArgb(20,166,132)));
        footer.Controls.Add(Action("Kijelölt sor törlése",(_,_)=>DeletePart(),Color.FromArgb(145,67,68)));
        pane.Controls.Add(_parts);pane.Controls.Add(footer);gbox.Controls.Add(pane);
        layout.Controls.Add(fields,0,0);layout.Controls.Add(sheet,0,1);layout.Controls.Add(gbox,0,2);
        tab.Controls.Add(layout);_tabs.TabPages.Add(tab);
    }
    private void InitPreviewTab()
    {
        var tab=new TabPage("02  Szabásterv"){BackColor=Color.White};
        var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=40,Padding=new Padding(10,3,0,0)};
        tools.Controls.Add(Tag("Tábla:"));tools.Controls.Add(_boardNumber);
        _strategy.Items.Add("Hagyományos sávozás (korábbi módszer)");
        _strategy.Items.Add("Anyagtakarékos – teljes átvágások (ÚJ)");
        _strategy.SelectedIndex=0;
        _strategy.SelectedIndexChanged+=(_,_)=>ShowSelectedPlan();
        tools.Controls.Add(Tag("Vágási mód:"));tools.Controls.Add(_strategy);
        tools.Controls.Add(Action("Maradéklista (CSV)",(_,_)=>SaveRemnants(),Color.FromArgb(80,111,107)));
        var hint=new Label{AutoSize=true,Text="Zöld: alkatrész; kék: sáv; szürke: maradék",Padding=new Padding(14,9,0,0)};
        tools.Controls.Add(hint);
        _boardNumber.ValueChanged+=(s,e)=>_canvas.Invalidate();
        tab.Controls.Add(_canvas);tab.Controls.Add(_summary);tab.Controls.Add(_comparison);tab.Controls.Add(tools);
        _tabs.TabPages.Add(tab);
    }
    private void InitMachineTab()
    {
        var tab=new TabPage("03  Gépi sorrend és export"){BackColor=Color.White};
        var notice=new Label{Dock=DockStyle.Top,Height=100,Padding=new Padding(12,12,12,8),
            Text="KÍSÉRLETI .xPrg EXPORT – NEM GÉPRE HITELESÍTETT!\n"+
                 "A műveletsor ELŐBB minden tábla sávozását, majd a csíkok visszaadását és hosszvágását mutatja. " +
                 "Az xPrg XML-szerkezete a kapott mintát követi, de a vezérlés fizikai sorrendje nem igazolt.\n"+
                 "Azonos csíkok csak javaslatként párosíthatók; egymásra rakás gépi parancsot NEM adunk ki.",
            BackColor=Color.FromArgb(255,242,225),ForeColor=Color.FromArgb(130,65,25)};
        _ops.Columns.Add("Order","Sorszám");_ops.Columns.Add("Phase","Művelet");
        _ops.Columns.Add("Board","Tábla");_ops.Columns.Add("Info","Részletek");
        _ops.Columns[3].FillWeight=300;
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(9,7,0,0)};
        footer.Controls.Add(Action(".xPrg + .xMix fájlok mentése (PRÓBA)",(_,_)=>ExportPlan(),Color.FromArgb(173,85,43)));
        tab.Controls.Add(_ops);tab.Controls.Add(footer);tab.Controls.Add(notice);
        _tabs.TabPages.Add(tab);
    }
    private void AddPart()
    {
        _tabs.SelectedIndex=0;
        int idx=_parts.Rows.Add("",0,0,1,"0-0");
        _parts.CurrentCell=_parts.Rows[idx].Cells[0];_parts.BeginEdit(true);
    }
    private void DeletePart()
    {
        if(_parts.CurrentRow is {IsNewRow:false} row)_parts.Rows.Remove(row);
    }
    private static double Parse(object? value,string title)
    {
        var text=Convert.ToString(value)?.Trim().Replace(',','.');
        if(!double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out double v)||!double.IsFinite(v))
            throw new ArgumentException("Hibás vagy hiányzó mező: "+title);
        return v;
    }
    private ProjectData ReadProject()
    {
        _parts.EndEdit();
        var p=new ProjectData{
            Furniture=_furniture.Text.Trim(),Destination=_destination.Text.Trim(),
            Material=string.IsNullOrWhiteSpace(_material.Text)?"Feya":_material.Text.Trim(),
            MachineMaterialCode=_machineCode.Text.Trim(),
            FurnitureWidth=(double)_n["fw"].Value,FurnitureHeight=(double)_n["fh"].Value,
            FurnitureDepth=(double)_n["fd"].Value,BoardLength=(double)_n["length"].Value,
            BoardWidth=(double)_n["width"].Value,Thickness=(double)_n["thick"].Value,
            Kerf=(double)_n["kerf"].Value,Trim=(double)_n["trim"].Value,
            SelectedMode=_strategy.SelectedIndex==1?"economy":"classic"};
        foreach(DataGridViewRow row in _parts.Rows)
        {
            if(row.IsNewRow)continue;
            var name=Convert.ToString(row.Cells[0].Value)?.Trim()??"";
            if(string.IsNullOrWhiteSpace(name)&&string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[1].Value))
                && string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[2].Value)))continue;
            var width=Parse(row.Cells[1].Value,"sávszélesség");
            var length=Parse(row.Cells[2].Value,"alkatrész hossza");
            var qty=Parse(row.Cells[3].Value,"darabszám");
            if(qty!=Math.Floor(qty)||qty<1||qty>2000)throw new ArgumentException("Hibás darabszám");
            p.Parts.Add(new PartSpec{Name=string.IsNullOrWhiteSpace(name)?"Alkatrész":name,
                Width=width,Length=length,Quantity=(int)qty,Edging=Convert.ToString(row.Cells[4].Value)??"0-0"});
        }
        return p;
    }
    private void LoadProject(ProjectData p)
    {
        _furniture.Text=p.Furniture;_destination.Text=p.Destination;_material.Text=p.Material;
        _machineCode.Text=p.MachineMaterialCode;
        void Set(string key,double val){var n=_n[key];n.Value=Math.Clamp((decimal)val,n.Minimum,n.Maximum);}
        Set("fw",p.FurnitureWidth);Set("fh",p.FurnitureHeight);Set("fd",p.FurnitureDepth);
        Set("length",p.BoardLength);Set("width",p.BoardWidth);Set("thick",p.Thickness);
        Set("kerf",p.Kerf);Set("trim",p.Trim);
        _parts.Rows.Clear();foreach(var part in p.Parts)_parts.Rows.Add(part.Name,part.Width,part.Length,part.Quantity,part.Edging);
        _plan=null;_classicPlan=null;_economyPlan=null;_ops.Rows.Clear();
        _summary.Text="";_comparison.Text="";
        _strategy.SelectedIndex=p.SelectedMode=="economy"?1:0;
        _canvas.Invalidate();
    }
    private void BuildPlan()
    {
        try
        {
            var p=ReadProject();
            _classicPlan=Planner.BuildClassic(p);
            _economyPlan=Planner.BuildEconomy(p);
            ShowSelectedPlan();
            _tabs.SelectedIndex=1;
            _status.Text="Mindkét szabásterv elkészült. Válaszd ki, melyiket szeretnéd használni.";
        }
        catch(Exception ex)
        {
            _plan=null;_classicPlan=null;_economyPlan=null;
            MessageBox.Show(this,ex.Message,"Tervezési hiba",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }

    private static string CompactPlan(CuttingPlan p)
    {
        string free=p.LargestRemnant is { } r
            ? $"legnagyobb maradék {r.Length:0}×{r.Width:0} mm"
            : "nincs 100×100 mm feletti maradék";
        return $"{p.Boards.Count} tábla • {p.Utilization:P1} kihozatal • {free}";
    }

    private void ShowSelectedPlan()
    {
        if(_classicPlan is null||_economyPlan is null)return;
        _plan=_strategy.SelectedIndex==1?_economyPlan:_classicPlan;
        _boardNumber.Minimum=1;
        _boardNumber.Maximum=Math.Max(1,_plan.Boards.Count);
        _boardNumber.Value=1;
        _comparison.Text="Hagyományos: "+CompactPlan(_classicPlan)+"\n"+
            "Anyagtakarékos: "+CompactPlan(_economyPlan)+"  |  A választás a kezelőé.";
        _summary.Text=$"KIVÁLASZTVA: {_plan.ModeName}     " +
            $"Táblák: {_plan.Boards.Count}     Sávok: {_plan.Boards.Sum(b=>b.Strips.Count)}     " +
            $"Darabok: {_plan.TotalParts}     " +
            $"Azonos csíkpárok: {_plan.MatchingStrips.Count} (csak javaslat)";
        _ops.Rows.Clear();foreach(var op in _plan.Operations)
            _ops.Rows.Add(op.Order,op.Phase,op.Board,op.Detail);
        _canvas.Invalidate();
        _status.Text="Teljes átvágásos Y-sávozás, majd külön levett csíkok X-darabolása. A gépi export kísérleti.";
    }

    private void SaveRemnants()
    {
        if(_plan is null)
        {
            MessageBox.Show(this,"Először készíts szabástervet!","Nincs szabásterv");
            return;
        }
        using var dlg=new SaveFileDialog { Filter="Excelben megnyitható CSV (*.csv)|*.csv",
            FileName="lapterv_felhasznalhato_maradekok.csv" };
        if(dlg.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            static string Csv(string v)=>"\""+v.Replace("\"","\"\"")+"\"";
            var lines=new List<string>{"Tábla;Hossz (mm);Szélesség (mm);Terület (m2);Megnevezés;Felhasználható"};
            foreach(var r in _plan.Remnants.OrderByDescending(r=>r.Area))
                lines.Add(string.Join(";",r.Board,
                    r.Length.ToString("0.##",CultureInfo.InvariantCulture),
                    r.Width.ToString("0.##",CultureInfo.InvariantCulture),
                    (r.Area/1_000_000d).ToString("0.###",CultureInfo.InvariantCulture),
                    Csv(r.Origin),r.Reusable?"igen (legalább 100×100 mm)":"apró maradék"));
            File.WriteAllLines(dlg.FileName,lines,System.Text.Encoding.UTF8);
            _status.Text="Táblánkénti maradéklista mentve: "+dlg.FileName;
        }
        catch(Exception ex)
        {
            MessageBox.Show(this,ex.Message,"Maradéklista mentési hiba",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }

    private void SaveProject()
    {
        try
        {
            using var d=new SaveFileDialog{Filter="Lapterv projekt (*.lapterv.json)|*.lapterv.json|JSON (*.json)|*.json",
                FileName="lapterv_projekt.lapterv.json"};
            if(d.ShowDialog(this)!=DialogResult.OK)return;
            File.WriteAllText(d.FileName,JsonSerializer.Serialize(ReadProject(),new JsonSerializerOptions{WriteIndented=true}));
            _status.Text="Projekt mentve: "+d.FileName;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Mentési hiba",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void OpenProject()
    {
        try
        {
            using var d=new OpenFileDialog{Filter="Lapterv projekt (*.json)|*.json"};
            if(d.ShowDialog(this)!=DialogResult.OK)return;
            var data=JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(d.FileName));
            if(data is null)throw new InvalidDataException("Üres projekt");
            LoadProject(data);_status.Text="Projekt betöltve: "+d.FileName;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Betöltési hiba",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void ExportPlan()
    {
        if(_plan is null){MessageBox.Show(this,"Először készíts szabástervet!","Nincs terv");return;}
        // Always recompute from current editing state; never export an out-of-date plan.
        try
        {
            var p=ReadProject();
            _plan=_strategy.SelectedIndex==1?Planner.BuildEconomy(p):Planner.BuildClassic(p);
            Planner.ValidateGeometry(_plan);
        }
        catch(Exception err){MessageBox.Show(this,err.Message,"Ellenőrzés sikertelen",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
        var msg=$"Kiválasztott vágási mód: {_plan.ModeName}\n\n"+
            "A fájl a kapott SCM XPrg MINTÁK SZERKEZETÉT követi, de a gépi parancsok\n"+
            "valós végrehajtása és az XMix kompatibilitása MÉG NEM IGAZOLT.\n\n"+
            "A fájl kísérleti prototípus; a valódi gépen csak gyártói szimuláció,\n"+
            "origó-, irány-, ütköző- és programellenőrzés UTÁN tesztelhető.\n"+
            "A csíkvágások közös sorrendjét az XML nem garantálja.\n\n"+
            "Ennek tudatában elkészíted a TESZT fájlokat?";
        if(MessageBox.Show(this,msg,"FIGYELEM – kísérleti gépi export",
            MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        if(string.IsNullOrWhiteSpace(_plan.Project.MachineMaterialCode))
        {
            MessageBox.Show(this,"Az .xPrg generálásához add meg az anyag pontos gépi azonosítóját a bútoradatlap tetején!\nPéldául a kapott mintákban BLAP003 volt, de ez nem biztos, hogy a te anyagod kódja.","Hiányzó gépi anyagkód",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            _tabs.SelectedIndex=0;return;
        }
        using var d=new SaveFileDialog{Filter="SCM xMix (próba) (*.xMix)|*.xMix",FileName="lapterv_PROBA.xMix"};
        if(d.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            string result=XprgExporter.Export(_plan,d.FileName);
            MessageBox.Show(this,result,"Gépi próbafájlok elkészültek",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            _status.Text="XPrg PRÓBA export: "+d.FileName;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Exporthiba",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void PaintBoard(object? sender,PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.White);
        if(_plan is null||_plan.Boards.Count==0)
        {
            TextRenderer.DrawText(g,"Adj hozzá alkatrészeket, majd kattints a „Szabásterv készítése” gombra.",
                Font,new Point(28,45),Color.FromArgb(90,105,120));return;
        }
        var p=_plan.Project;int idx=Math.Clamp((int)_boardNumber.Value-1,0,_plan.Boards.Count-1);
        var board=_plan.Boards[idx];
        float scl=Math.Min((_canvas.ClientSize.Width-115)/(float)p.BoardLength,(_canvas.ClientSize.Height-105)/(float)p.BoardWidth);
        scl=Math.Max(.05f,scl);float x0=55,y0=45;
        g.FillRectangle(Brushes.LightGray,x0,y0,(float)p.BoardLength*scl,(float)p.BoardWidth*scl);
        g.DrawRectangle(Pens.Black,x0,y0,(float)p.BoardLength*scl,(float)p.BoardWidth*scl);
        using var alternate=new SolidBrush(Color.FromArgb(213,237,245));
        using var bandBorder=new Pen(Color.FromArgb(30,90,123),1.2f);
        using var pieceBrush=new SolidBrush(Color.FromArgb(144,207,174));
        using var pieceBorder=new Pen(Color.FromArgb(30,114,81),1.2f);
        // A vágások a teljes aktuális tábla/sáv keresztmetszetén futnak.
        foreach(var band in board.Strips)
        {
            var bx=x0+(float)p.Trim*scl;
            float by=y0+(float)band.Y*scl,bw=(float)(p.BoardLength-2*p.Trim)*scl,bh=(float)band.Width*scl;
            g.FillRectangle(alternate,bx,by,bw,bh);g.DrawRectangle(bandBorder,bx,by,bw,bh);
            foreach(var part in band.Pieces)
            {
                float px=x0+(float)part.X*scl,py=by;
                var rect=new RectangleF(px,py,Math.Max(1,(float)part.Length*scl),Math.Max(1,(float)part.Width*scl));
                g.FillRectangle(pieceBrush,rect);g.DrawRectangle(pieceBorder,rect.X,rect.Y,rect.Width,rect.Height);
                if(rect.Width>65&&rect.Height>23)
                {
                    string label=$"{part.Part.Name}  {part.Length:0}×{part.Width:0}";
                    TextRenderer.DrawText(g,label,new Font("Segoe UI",8),Rectangle.Ceiling(rect),Color.FromArgb(18,63,49),
                        TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
                }
            }
        }
        // A nagy, egybefüggő hasznosítható maradékot külön jelöljük.
        foreach(var rem in _plan.Remnants.Where(r=>r.Board==board.Number))
        {
            if(!rem.Reusable)continue;
            using var textBrush=new SolidBrush(Color.FromArgb(50,53,71));
            var label=$"Maradék: {rem.Length:0}×{rem.Width:0} mm";
            if(rem.Origin.StartsWith("Teljes",StringComparison.Ordinal))
            {
                float y=(float)(p.BoardWidth-p.Trim-rem.Width)*scl+y0;
                TextRenderer.DrawText(g,label,Font,new Point((int)(x0+10),(int)(y+5)),textBrush.Color);
            }
        }
        TextRenderer.DrawText(g,$"{p.BoardLength:0} mm",Font,new Point((int)(x0+30),15),Color.Black);
        TextRenderer.DrawText(g,$"{p.BoardWidth:0} mm",Font,new Point(7,(int)(y0+25)),Color.Black);
    }
}
