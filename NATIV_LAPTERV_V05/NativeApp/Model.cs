using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
namespace LaptervNative;
public sealed class PartSpec
{
    public string Name { get; set; } = "Alkatrész";
    public double Width { get; set; }
    public double Length { get; set; }
    public int Quantity { get; set; } = 1;
    public string Edging { get; set; } = "0-0";
}
public sealed class ProjectData
{
    public string Furniture { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Material { get; set; } = "Feya";
    public string MachineMaterialCode { get; set; } = "";
    public double FurnitureWidth { get; set; } = 1000;
    public double FurnitureHeight { get; set; } = 910;
    public double FurnitureDepth { get; set; } = 600;
    public double BoardLength { get; set; } = 2800;
    public double BoardWidth { get; set; } = 2070;
    public double Thickness { get; set; } = 18;
    public double Kerf { get; set; } = 5;
    public double Trim { get; set; } = 10;
    public string SelectedMode { get; set; } = "classic";
    public List<PartSpec> Parts { get; set; } = new();
}
public sealed class Piece
{
    public required PartSpec Part { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Length { get; init; }
}
public sealed class StripPlan
{
    public double Width { get; set; }
    public double Y { get; set; }
    public List<Piece> Pieces { get; } = new();
    public string Signature => string.Join(";",Pieces.Select(p=>p.Length.ToString("F3",CultureInfo.InvariantCulture)))+"|"+Width.ToString("F3",CultureInfo.InvariantCulture);
}
public sealed class BoardPlan
{
    public int Number { get; set; }
    public List<StripPlan> Strips { get; } = new();
}
public sealed class ProductionOp
{
    public int Order { get; set; }
    public string Phase { get; set; } = "";
    public int Board { get; set; }
    public string Detail { get; set; } = "";
}
public sealed class Remnant
{
    public int Board { get; init; }
    public double Length { get; init; }
    public double Width { get; init; }
    public string Origin { get; init; } = "";
    public double Area => Length * Width;
    public bool Reusable => Math.Min(Width, Length) >= 100;
}
public sealed class CuttingPlan
{
    public string ModeName { get; set; } = "Hagyományos sávozás";
    public List<Remnant> Remnants { get; } = new();
    public double LargestRemnantArea => Remnants.Where(r => r.Reusable).Select(r => r.Area).DefaultIfEmpty().Max();
    public double ReusableRemnantArea => Remnants.Where(r => r.Reusable).Sum(r => r.Area);
    public double PartArea => Boards.Sum(b => b.Strips.Sum(s => s.Pieces.Sum(q => q.Length * q.Width)));
    public double Utilization => Boards.Count == 0 ? 0 : PartArea / (Boards.Count * Project.BoardLength * Project.BoardWidth);
    public Remnant? LargestRemnant => Remnants.Where(r => r.Reusable).OrderByDescending(r => r.Area).FirstOrDefault();
    public required ProjectData Project { get; init; }
    public List<BoardPlan> Boards { get; } = new();
    public List<ProductionOp> Operations { get; } = new();
    public List<(int Board1,int Strip1,int Board2,int Strip2)> MatchingStrips { get; } = new();
    public int TotalParts => Boards.Sum(b=>b.Strips.Sum(s=>s.Pieces.Count));
}
