using BuildingModel.Diagnostics;
using ShearWallCalculator;
using ShearWallCalculator.ModelAdapter;
using calculator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The Loads overlay (off until asked for): one load case on the level shown. Orange arrows: the shear each wall of the
    /// level above puts into this floor (at the upper wall's middle); blue arrows: what the top of each wall of this level
    /// receives from the diaphragm (the opposite of the wall's force on it). A legend gives the case's own wind, the part from
    /// above and Mt,comb. The case is picked from the ▾ menu or on the Calculations tab (the two follow each other); with
    /// none picked, the case that designs the most walls on this level.
    /// </summary>
    public partial class MainWindow
    {
        private bool showLoads;
        private string loadsCase; // as listed ("Case 2, ..." or, for an envelope, "Case 2, ... (rigid)"); null: the governing one

        private static readonly Brush AboveInk = Controls.Ui.Frozen("#D9480F"), TopInk = Controls.Ui.Frozen("#0B7285");
        private static readonly Pen AboveArrowPen = MakePen(AboveInk, 2.2), TopArrowPen = MakePen(TopInk, 2.2);

        /// <summary>The case the overlay shows on this level: the one picked if this level has it, else the one designing the most walls.</summary>
        private string LoadsCaseHere()
        {
            var names = CaseBreakdown.CaseNames(Calculator);
            if (loadsCase != null && names.Contains(loadsCase)) return loadsCase;
            return Calculator.GoverningLoadCase.Values.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(names.Contains)
                   ?? names.FirstOrDefault();
        }

        private void btnLoads_Click(object sender, RoutedEventArgs e)
        {
            showLoads = !showLoads;
            btnLoads.Tag = showLoads ? "on" : null;
            Log.Info("Loads", showLoads ? $"Loads overlay on: {LoadsCaseHere() ?? "no load cases"} on {LevelName(currentLevel)}." : "Loads overlay off.");
            RedrawDrawing();
        }

        /// <summary>▾: the load cases of this level; picking one shows it (and on the Calculations tab).</summary>
        private void btnLoadsOptions_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom, MaxHeight = 600 };
            string now = LoadsCaseHere();
            var names = CaseBreakdown.CaseNames(Calculator);
            if (names.Count == 0) menu.Items.Add(new MenuItem { Header = "No load cases calculated on this level", IsEnabled = false });
            foreach (var n in names)
            {
                var mi = new MenuItem { Header = n, IsCheckable = true, IsChecked = n == now };
                mi.Click += (s, a) => ShowLoadsCase(n, fromPanel: false);
                menu.Items.Add(mi);
            }
            menu.IsOpen = true;
        }

        /// <summary>Shows a case on the plan (from the ▾ menu, or the case picked on the Calculations tab).</summary>
        private void ShowLoadsCase(string listed, bool fromPanel)
        {
            if (listed == null) return; // "all load cases" on the Calculations tab: the overlay keeps its case
            loadsCase = listed;
            if (!fromPanel)
            {
                showLoads = true;
                btnLoads.Tag = "on";
                resultsPanel?.ChooseCase(listed);
            }
            Log.Info("Loads", $"Loads overlay: {listed} on {LevelName(currentLevel)}.");
            if (showLoads) RedrawDrawing();
        }

        /// <summary>"level 3, wall 6" (a reaction's source) → the upper wall's model ID ("W6").</summary>
        private static string UpperWallId(string from, StoryMap upper)
        {
            var m = Regex.Match(from ?? "", @"wall (\d+)$");
            if (!m.Success) return from ?? "";
            int k = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            return upper != null && upper.Walls.TryGetValue(k, out var id) ? id : $"wall {k}";
        }

        private void DrawLoads(DrawingContext ctx)
        {
            if (!showLoads || foundationView || Calculator?._wall_system == null) return;
            double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            string name = LoadsCaseHere();
            var cs = name == null ? null : CaseBreakdown.Case(Calculator, name);
            if (cs == null) { Legend(ctx, new[] { "Loads: no load cases calculated on this level." }, dip); return; }
            var (above, totals, lc) = cs.Value;
            var walls = Calculator._wall_system._walls;
            var upper = currentLevel + 1 < stories.Count ? stories[currentLevel + 1] : null;

            double max = above.Select(p => Math.Sqrt(p.Fx * p.Fx + p.Fy * p.Fy)).Concat(totals.Values.Select(Math.Abs)).DefaultIfEmpty(0).Max();
            double Len(double f) => 18 + 52 * Math.Abs(f) / Math.Max(max, 1e-9);

            // the upper walls' shears into this floor: on one side of the line, the wall tops' on the other (they share lines)
            foreach (var p in above)
            {
                double f = Math.Sqrt(p.Fx * p.Fx + p.Fy * p.Fy);
                if (f < 1e-6) continue;
                Arrow(ctx, new Point(p.X, p.Y), new Vector(p.Fx / f, p.Fy / f), Len(f), +16, AboveArrowPen, AboveInk,
                      $"{UpperWallId(p.From, upper)} {f:0.00} k", dip);
            }
            foreach (var kv in totals)
            {
                if (!walls.TryGetValue(kv.Key, out var w)) continue;
                double onTop = -kv.Value; // the diaphragm pushes the wall top opposite to the wall's force on it
                if (Math.Abs(onTop) < 1e-6) continue;
                bool ew = w.WallDir == WallDirs.EastWest;
                var dir = ew ? new Vector(Math.Sign(onTop), 0) : new Vector(0, Math.Sign(onTop));
                string id = CurrentStory != null && CurrentStory.Walls.TryGetValue(kv.Key, out var mid) ? mid : $"W{kv.Key}";
                Arrow(ctx, new Point((w.Start.X + w.End.X) / 2, (w.Start.Y + w.End.Y) / 2), dir, Len(onTop), -16, TopArrowPen, TopInk,
                      $"{id} top {Math.Abs(onTop):0.00} k", dip);
            }

            var row = CaseBreakdown.Rows(Calculator).FirstOrDefault(r => r.Case == name);
            string upperName = currentLevel + 1 < stories.Count ? stories[currentLevel + 1].Level.Name : "the level above";
            var lines = new List<string>
            {
                $"Loads · {name}" + (CaseBreakdown.WindFrom(lc) is string from ? $"  ·  wind from {from}" : ""),
                $"This level's wind:  Vx {Num(lc?.Vx - lc?.AboveVx)} k,  Vy {Num(lc?.Vy - lc?.AboveVy)} k,  Mt {Num(lc?.Mt - lc?.AboveMt, "0.0")} k·ft",
                above.Count > 0
                    ? $"From {upperName} (orange, its walls on this floor):  Vx {Num(lc?.AboveVx)} k,  Vy {Num(lc?.AboveVy)} k,  Mt {Num(lc?.AboveMt, "0.0")} k·ft about the CM"
                    : $"Nothing from above ({(currentLevel + 1 < stories.Count ? upperName + " has no walls in this case" : "the roof level")}).",
                $"On the diaphragm:  Vx {Num(lc?.Vx)} k,  Vy {Num(lc?.Vy)} k" + (row?.MtComb != null ? $";  Mt,comb {Num(row.MtComb, "0.0")} k·ft about the CR" : ""),
                "Blue: what the top of each wall here receives (it passes it down to its base).",
            };
            DrawWind(ctx, lc, dip);
            Legend(ctx, lines, dip);
        }

        private static string Num(double? v, string f = "0.00") => v.HasValue ? v.Value.ToString(f, CultureInfo.CurrentCulture) : "–";

        private static readonly Brush WindInk = Controls.Ui.Frozen("#B01E5A8C");
        private static readonly Pen WindPen = MakePen(WindInk, 7), CurlPen = MakePen(WindInk, 3);

        /// <summary>
        /// The wind: a broad arrow outside the windward side of the building pointing at it ("WIND from SW"), and the case's
        /// torsion as a curl at the building's center, counter-clockwise for + Mt.
        /// </summary>
        private void DrawWind(DrawingContext ctx, LateralLoadCase lc, double dip)
        {
            if (!(CaseBreakdown.WindDir(lc) is Vector d)) return;
            var box = Calculator.BoundingBoxWorld;
            if (box.IsEmpty || box.Width <= 0 || box.Height <= 0) return;
            var a = WorldToScreen(box.TopLeft, m_layers);
            var b = WorldToScreen(box.BottomRight, m_layers);
            var screenBox = new Rect(a, b);
            var c = new Point(screenBox.X + screenBox.Width / 2, screenBox.Y + screenBox.Height / 2);
            var sd = WorldToScreen(new Point(box.X + box.Width / 2, box.Y + box.Height / 2) + d, m_layers) - WorldToScreen(new Point(box.X + box.Width / 2, box.Y + box.Height / 2), m_layers);
            if (sd.Length < 1e-9) return;
            sd.Normalize();
            // from the center back against the wind to the box edge, then clear of it
            double tx = Math.Abs(sd.X) > 1e-9 ? screenBox.Width / 2 / Math.Abs(sd.X) : double.MaxValue;
            double ty = Math.Abs(sd.Y) > 1e-9 ? screenBox.Height / 2 / Math.Abs(sd.Y) : double.MaxValue;
            var head = c - sd * (Math.Min(tx, ty) + 28);
            var tail = head - sd * 95;
            var perp = new Vector(-sd.Y, sd.X);
            ctx.DrawLine(WindPen, tail, head - sd * 14);
            var g = new StreamGeometry();
            using (var o = g.Open())
            {
                o.BeginFigure(head, true, true);
                o.PolyLineTo(new[] { head - sd * 22 + perp * 13, head - sd * 22 - perp * 13 }, true, false);
            }
            g.Freeze();
            ctx.DrawGeometry(WindInk, null, g);
            var label = new FormattedText($"WIND from {CaseBreakdown.WindFrom(lc)}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                          new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 13, WindInk, dip);
            var at = tail - sd * 8 - new Vector(label.Width / 2, label.Height / 2) + new Vector(-sd.X * label.Width / 2, -sd.Y * label.Height / 2);
            ctx.DrawRectangle(LabelBack, null, new Rect(at.X - 3, at.Y, label.Width + 6, label.Height));
            ctx.DrawText(label, at);

            // the case's own torsion (Cases 2 and 4): + is counter-clockwise on the plan
            double mt = lc.Mt - lc.AboveMt;
            if (Math.Abs(mt) < 1e-6) return;
            bool ccwPlan = mt > 0;
            // y is up in the plan and down on the screen, so a counter-clockwise plan turn is clockwise on the screen
            var flipped = WorldToScreen(new Point(0, 1), m_layers).Y < WorldToScreen(new Point(0, 0), m_layers).Y;
            bool ccwScreen = flipped ? ccwPlan : !ccwPlan;
            double r = 30;
            Point P(double deg) { double t = deg * Math.PI / 180; return new Point(c.X + r * Math.Cos(t), c.Y - r * Math.Sin(t)); } // screen, y down
            double start = ccwScreen ? -60 : 240, end = ccwScreen ? 210 : -30;
            var arc = new StreamGeometry();
            using (var o = arc.Open())
            {
                o.BeginFigure(P(start), false, false);
                o.ArcTo(P(end), new Size(r, r), 0, true, ccwScreen ? SweepDirection.Counterclockwise : SweepDirection.Clockwise, true, false);
            }
            arc.Freeze();
            ctx.DrawGeometry(null, CurlPen, arc);
            // the arrowhead at the end, along the tangent
            double te = end * Math.PI / 180;
            var tangent = ccwScreen ? new Vector(-Math.Sin(te), -Math.Cos(te)) : new Vector(Math.Sin(te), Math.Cos(te));
            var e = P(end);
            var n = new Vector(-tangent.Y, tangent.X);
            var hg = new StreamGeometry();
            using (var o = hg.Open())
            {
                o.BeginFigure(e + tangent * 8, true, true);
                o.PolyLineTo(new[] { e - tangent * 5 + n * 6, e - tangent * 5 - n * 6 }, true, false);
            }
            hg.Freeze();
            ctx.DrawGeometry(WindInk, null, hg);
            Label(ctx, $"Mt {Math.Abs(mt):0.0} k·ft {(ccwPlan ? "CCW" : "CW")}", new Point(c.X + r + 6, c.Y - r - 4), WindInk, dip);
        }

        /// <summary>An arrow of <paramref name="px"/> pixels centered on a plan point, along a plan direction, set off the line by <paramref name="side"/> px.</summary>
        private void Arrow(DrawingContext ctx, Point at, Vector dir, double px, double side, Pen pen, Brush ink, string label, double dip)
        {
            var s0 = WorldToScreen(at, m_layers);
            var sd = WorldToScreen(at + dir, m_layers) - s0;
            if (sd.Length < 1e-9) return;
            sd.Normalize();
            var perp = new Vector(-sd.Y, sd.X);
            var c = s0 + perp * side;
            var tail = c - sd * px / 2;
            var head = c + sd * px / 2;
            ctx.DrawLine(pen, tail, head);
            var g = new StreamGeometry();
            using (var o = g.Open())
            {
                o.BeginFigure(head, true, true);
                o.PolyLineTo(new[] { head - sd * 9 + perp * 4.5, head - sd * 9 - perp * 4.5 }, true, false);
            }
            g.Freeze();
            ctx.DrawGeometry(ink, null, g);
            Label(ctx, label, c + perp * (side > 0 ? 4 : -18) + new Vector(4, 0), ink, dip);
        }

        /// <summary>The overlay's legend, bottom left of the plan.</summary>
        private void Legend(DrawingContext ctx, IList<string> lines, double dip)
        {
            var texts = lines.Select((t, i) => new FormattedText(t, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"),
                                                                 i == 0 ? 12 : 11, i == 0 ? Controls.Ui.Ink : Controls.Ui.InkSecondary, dip)).ToList();
            if (texts.Count > 0) texts[0].SetFontWeight(FontWeights.SemiBold);
            double w = texts.Max(t => t.Width) + 16, h = texts.Sum(t => t.Height + 2) + 12;
            double y = Math.Max(8, m_layers.ActualHeight - h - 12);
            ctx.DrawRectangle(LabelBack, MakePen(Controls.Ui.Hairline, 1), new Rect(10, y, w, h));
            double ty = y + 6;
            foreach (var t in texts) { ctx.DrawText(t, new Point(18, ty)); ty += t.Height + 2; }
        }

    }
}
