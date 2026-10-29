using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace LaptervNative;

/// <summary>
/// Kétlépcsős guillotine tervezés: 1) teljes tábla sávozása (Y),
/// 2) a külön levett csík teljes keresztvágása (X).
/// Nincs a tábla közepén megálló, szabadon induló vágás.
/// A gyártói gépi XPrg-végrehajtási sorrend külön hitelesítendő.
/// </summary>
public static class Planner
{
    private const double Eps = 0.0001;
    public static CuttingPlan Build(ProjectData p) => BuildClassic(p);

    // Az eredeti 0.4-es konzervatív séma változatlan kiosztási szabályokkal.
    public static CuttingPlan BuildClassic(ProjectData p)
    {
        Check(p);
        var plan = new CuttingPlan { Project = p, ModeName = "Hagyományos sávozás" };
        var instances = p.Parts.SelectMany(x => Enumerable.Repeat(x, x.Quantity))
            .OrderByDescending(x => x.Width).ThenByDescending(x => x.Length).ToList();
        foreach (var part in instances)
        {
            StripPlan? selected = null;
            BoardPlan? board = null;
            foreach (var b in plan.Boards)
            {
                var s = b.Strips.FirstOrDefault(q => Near(q.Width, part.Width) &&
                    NextX(q, p) + part.Length <= p.BoardLength - p.Trim + Eps);
                if (s is not null) { board = b; selected = s; break; }
            }
            if (selected is null)
            {
                board = plan.Boards.FirstOrDefault(b => NextY(b, p) + part.Width <= p.BoardWidth - p.Trim + Eps);
                if (board is null)
                {
                    board = new BoardPlan { Number = plan.Boards.Count + 1 };
                    plan.Boards.Add(board);
                }
                selected = new StripPlan { Width = part.Width, Y = NextY(board, p) };
                board.Strips.Add(selected);
            }
            selected.Pieces.Add(new Piece { Part = part, X = NextX(selected, p), Y = selected.Y,
                Width = part.Width, Length = part.Length });
        }
        FinalizePlan(plan);
        return plan;
    }

    /// <summary>
    /// Anyagtakarékos mód, több determinisztikus jelöltből választ.
    /// Először azonos sávszélességű tételek 1D hosszvágási optimalizálása;
    /// utána a kialakult különböző szélességű csíkok 1D táblakiosztása.
    /// Mindegyik művelet teljes keresztmetszetű guillotine-vágással előállítható.
    /// Ez heurisztika, nem a globális matematikai optimum bizonyítása.
    /// </summary>
    public static CuttingPlan BuildEconomy(ProjectData p)
    {
        Check(p);
        // A gazdaságos mód soha nem lehet rosszabb a hagyományosnál.
        CuttingPlan? best = BuildClassic(p);
        // BFD, FFD, növekvő és váltakozó hossz-sorrend; többféle tábla-rendezés.
        for (int order = 0; order < 4; order++)
            for (int boardOrder = 0; boardOrder < 3; boardOrder++)
            {
                var candidate = BuildCandidate(p, order, boardOrder);
                if (best is null || Compare(candidate, best) < 0) best = candidate;
            }
        if (best is null) throw new InvalidOperationException("Nem sikerült szabástervet készíteni.");
        best.ModeName = "Anyagtakarékos – teljes átvágások";
        ValidateGeometry(best);
        return best;
    }

    private sealed class StripBucket
    {
        public required double Width { get; init; }
        public List<PartSpec> Parts { get; } = new();
        public double UsedLength { get; set; }
        public double UsedArea => Parts.Sum(p => p.Width * p.Length);
    }

    private static CuttingPlan BuildCandidate(ProjectData p, int itemOrder, int boardOrder)
    {
        var strips = new List<StripBucket>();
        foreach (var group in p.Parts.SelectMany(x => Enumerable.Repeat(x, x.Quantity))
                     .GroupBy(x => x.Width).OrderByDescending(g => g.Key))
        {
            var items = group.OrderByDescending(x => x.Length).ToList();
            if (itemOrder == 2) items.Reverse();
            if (itemOrder == 3)
            {
                var zig = new List<PartSpec>(items.Count);
                for (int i = 0, j = items.Count - 1; i <= j; i++, j--)
                {
                    zig.Add(items[i]);
                    if (i < j) zig.Add(items[j]);
                }
                items = zig;
            }
            var bins = new List<StripBucket>();
            foreach (var part in items)
            {
                StripBucket? target = null;
                double bestFit = itemOrder == 1 ? double.NegativeInfinity : double.PositiveInfinity;
                foreach (var bucket in bins)
                {
                    double next = bucket.UsedLength + p.Kerf + part.Length;
                    double free = (p.BoardLength - 2 * p.Trim) - next;
                    if (free < -Eps) continue;
                    if (itemOrder == 1 ? free > bestFit : free < bestFit)
                    {
                        bestFit = free;
                        target = bucket;
                    }
                }
                if (target is null)
                {
                    target = new StripBucket { Width = part.Width };
                    bins.Add(target);
                    target.UsedLength = part.Length;
                }
                else target.UsedLength += p.Kerf + part.Length;
                target.Parts.Add(part);
            }
            strips.AddRange(bins);
        }
        IEnumerable<StripBucket> ordered = boardOrder switch
        {
            1 => strips.OrderByDescending(s => s.Width)
                .ThenByDescending(s => s.UsedArea),
            2 => strips.OrderByDescending(s => s.Width)
                .ThenBy(s => s.UsedLength),
            _ => strips.OrderByDescending(s => s.Width)
                .ThenByDescending(s => s.UsedLength)
        };
        var plan = new CuttingPlan { Project = p, ModeName = "Anyagtakarékos – teljes átvágások" };
        foreach (var source in ordered)
        {
            BoardPlan? target = null;
            double bestFree = double.PositiveInfinity;
            foreach (var board in plan.Boards)
            {
                double free = (p.BoardWidth - p.Trim) - NextY(board, p) - source.Width;
                if (free < -Eps) continue;
                if (free < bestFree) { target = board; bestFree = free; }
            }
            if (target is null)
            {
                target = new BoardPlan { Number = plan.Boards.Count + 1 };
                plan.Boards.Add(target);
            }
            var strip = new StripPlan { Width = source.Width, Y = NextY(target, p) };
            target.Strips.Add(strip);
            foreach (var part in source.Parts)
                strip.Pieces.Add(new Piece { Part = part, X = NextX(strip, p), Y = strip.Y,
                    Width = part.Width, Length = part.Length });
        }
        FinalizePlan(plan);
        return plan;
    }

    private static int Compare(CuttingPlan a, CuttingPlan b)
    {
        int c = a.Boards.Count.CompareTo(b.Boards.Count);
        if (c != 0) return c;
        // Ha ugyanannyi tábla kell, inkább egyben maradjon a maradék, ne apró hulladék legyen.
        c = b.LargestRemnantArea.CompareTo(a.LargestRemnantArea);
        if (c != 0) return c;
        c = b.ReusableRemnantArea.CompareTo(a.ReusableRemnantArea);
        if (c != 0) return c;
        return a.Boards.Sum(x => x.Strips.Count).CompareTo(b.Boards.Sum(x => x.Strips.Count));
    }

    public static void ValidateGeometry(CuttingPlan plan)
    {
        var p = plan.Project;
        int quantity = 0;
        var actual = new Dictionary<PartSpec, int>();
        foreach (var board in plan.Boards)
        {
            double nextY = p.Trim;
            foreach (var strip in board.Strips)
            {
                if (strip.Width <= 0 || !Near(strip.Y, nextY) ||
                    strip.Y + strip.Width > p.BoardWidth - p.Trim + Eps)
                    throw new InvalidOperationException("Nem teljes szélességben vágható sávméret / sávütközés.");
                nextY = strip.Y + strip.Width + p.Kerf;
                double nextX = p.Trim;
                foreach (var piece in strip.Pieces)
                {
                    if (!Near(piece.X, nextX) || !Near(piece.Y, strip.Y) ||
                        !Near(piece.Width, strip.Width) ||
                        !Near(piece.Length, piece.Part.Length) ||
                        piece.X + piece.Length > p.BoardLength - p.Trim + Eps)
                        throw new InvalidOperationException("A hosszvágás kilóg vagy ütközik egy darabbal.");
                    nextX = piece.X + piece.Length + p.Kerf;
                    actual[piece.Part] = actual.GetValueOrDefault(piece.Part) + 1;
                    quantity++;
                }
            }
        }
        if (quantity != plan.Project.Parts.Sum(x => x.Quantity) ||
            p.Parts.Any(x => actual.GetValueOrDefault(x) != x.Quantity))
            throw new InvalidOperationException("Az optimalizáló kihagyott vagy ismételt alkatrészt.");
    }

    private static void FinalizePlan(CuttingPlan plan)
    {
        ValidateGeometry(plan);
        var p = plan.Project;
        int number = 0;
        // SÁVOZÁS: a fűrész a teljes még egybefüggő táblaél mentén átvág.
        foreach (var board in plan.Boards)
        {
            if (p.Trim > 0)
                plan.Operations.Add(new ProductionOp { Order = ++number, Phase = "Sávozás",
                    Board = board.Number, Detail = $"{p.Trim:0.##} mm kezdő szélezés (teljes Y átvágás)" });
            foreach (var strip in board.Strips)
                plan.Operations.Add(new ProductionOp { Order = ++number, Phase = "Sávozás",
                    Board = board.Number, Detail = $"{strip.Width:0.##} mm-es teljes sáv, Y={strip.Y:0.##}; {strip.Pieces.Count} alkatrész" });
        }
        // HOSSZVÁGÁS: csak a már leválasztott csík kerül vissza.
        var all = plan.Boards.SelectMany(b => b.Strips.Select((s, i) => (Board: b, Strip: s, Index: i))).ToList();
        var matched = new HashSet<int>();
        for (int i = 0; i < all.Count; i++)
        {
            if (matched.Contains(i)) continue;
            for (int j = i + 1; j < all.Count; j++)
                if (!matched.Contains(j) && all[i].Strip.Signature == all[j].Strip.Signature)
                {
                    plan.MatchingStrips.Add((all[i].Board.Number, all[i].Index + 1,
                        all[j].Board.Number, all[j].Index + 1));
                    matched.Add(i); matched.Add(j); break;
                }
        }
        foreach (var board in plan.Boards)
            foreach (var pair in board.Strips.Select((s, i) => (Strip: s, Index: i)))
            {
                plan.Operations.Add(new ProductionOp { Order = ++number, Phase = "Hosszvágás",
                    Board = board.Number,
                    Detail = $"{pair.Index + 1}. leválasztott sáv visszaadása; teljes sáv átvágása, {p.Trim:0.##} mm X-szélezés" });
                foreach (var piece in pair.Strip.Pieces)
                    plan.Operations.Add(new ProductionOp { Order = ++number, Phase = "Hosszvágás",
                        Board = board.Number, Detail = $"{piece.Length:0.##} mm – {piece.Part.Name}" });
            }
        // Megmaradó, téglalap alakú, újrafelhasználható maradékok.
        foreach (var board in plan.Boards)
        {
            double leftY = p.BoardWidth - p.Trim - NextY(board, p);
            if (leftY > Eps) plan.Remnants.Add(new Remnant { Board = board.Number,
                Length = p.BoardLength - 2 * p.Trim, Width = leftY, Origin = "Teljes hosszúságú táblamaradék" });
            foreach (var strip in board.Strips)
            {
                double leftX = p.BoardLength - p.Trim - NextX(strip, p);
                if (leftX > Eps) plan.Remnants.Add(new Remnant { Board = board.Number,
                    Length = leftX, Width = strip.Width, Origin = "Sávvégi maradék" });
            }
        }
    }

    private static bool Near(double a, double b) => Math.Abs(a - b) < Eps;
    private static double NextX(StripPlan s, ProjectData p) => s.Pieces.Count == 0 ? p.Trim :
        s.Pieces[^1].X + s.Pieces[^1].Length + p.Kerf;
    private static double NextY(BoardPlan b, ProjectData p) => b.Strips.Count == 0 ? p.Trim :
        b.Strips[^1].Y + b.Strips[^1].Width + p.Kerf;

    public static void Check(ProjectData p)
    {
        if (p.BoardLength < 100 || p.BoardWidth < 100 || p.BoardLength > 10000 || p.BoardWidth > 10000)
            throw new ArgumentException("Érvénytelen táblaméret (100–10000 mm).");
        if (p.Thickness <= 0 || p.Thickness > 100 || p.Kerf <= 0 || p.Kerf > 20 || p.Trim < 0 || p.Trim > 100)
            throw new ArgumentException("Érvénytelen vastagság, fűrészrés vagy szélezés.");
        if (p.Parts.Count == 0 || p.Parts.Sum(x => x.Quantity) > 2000)
            throw new ArgumentException("Adj meg 1–2000 legyártandó alkatrészt.");
        foreach (var v in p.Parts)
        {
            if (!double.IsFinite(v.Width) || !double.IsFinite(v.Length) || v.Width <= 0 || v.Length <= 0 || v.Quantity < 1 || v.Quantity > 2000)
                throw new ArgumentException("Hibás alkatrészméret vagy darabszám: " + v.Name);
            // Szálirányt és élzárási oldalt megőrizzük: ez a mód NEM forgatja el az alkatrészt.
            if (v.Width + p.Kerf > p.BoardWidth - 2 * p.Trim + Eps ||
                v.Length + p.Kerf > p.BoardLength - 2 * p.Trim + Eps)
                throw new ArgumentException("Nem fér a táblára a szélezéssel és vágásréssel: " + v.Name);
        }
    }
}
