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
            var listing = CaseBreakdown.Listing(Calculator);
            if (listing.Count == 0) menu.Items.Add(new MenuItem { Header = "No load cases calculated on this level", IsEnabled = false });
            // one submenu per wind direction (W, SW, S, ...), its cases inside: a case is found by the side the wind is on
            foreach (var g in listing.GroupBy(i => i.From))
            {
                bool here = g.Any(i => i.Name == now);
                var sub = new MenuItem
                {
                    Header = (g.Key.Length > 0 ? $"Wind from {g.Key}" : "No load") + $"   ({g.Count()})",
                    FontWeight = here ? FontWeights.SemiBold : FontWeights.Normal,
                };
                foreach (var i in g)
                {
                    var mi = new MenuItem { Header = i.Label, IsCheckable = true, IsChecked = i.Name == now, ToolTip = i.Name, FontWeight = FontWeights.Normal };
                    string name = i.Name;
                    mi.Click += (s, a) => ShowLoadsCase(name, fromPanel: false);
                    sub.Items.Add(mi);
                }
                menu.Items.Add(sub);
            }
            menu.IsOpen = true;
        }

        /// <summary>The case's label on this level ("SW · Case 4, torsion ..."), or its name.</summary>
        private string CaseLabel(string name) => CaseBreakdown.Listing(Calculator).FirstOrDefault(i => i.Name == name)?.Label ?? name;

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
            Log.Info("Loads", $"Loads overlay: {CaseLabel(listed)} on {LevelName(currentLevel)} ({listed}).");
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

        /// <summary>The wind arrow and its label on screen this frame (the legend keeps off it); empty when none.</summary>
        private Rect windRect = Rect.Empty;

        private void DrawLoads(DrawingContext ctx)
        {
            windRect = Rect.Empty;
            loadsKey = null;
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
                // it may slide along its wall, staying within the wall
                double wallPx = (WorldToScreen(w.End, m_layers) - WorldToScreen(w.Start, m_layers)).Length;
                Arrow(ctx, new Point((w.Start.X + w.End.X) / 2, (w.Start.Y + w.End.Y) / 2), dir, Len(onTop), -16, TopArrowPen, TopInk,
                      $"{id} top {Math.Abs(onTop):0.00} k", dip, Math.Max(0, wallPx / 2 - Len(onTop) / 2));
            }

            var row = CaseBreakdown.Rows(Calculator).FirstOrDefault(r => r.Case == name);
            string upperName = currentLevel + 1 < stories.Count ? stories[currentLevel + 1].Level.Name : "the level above";
            var lines = new List<string>
            {
                $"Loads · {CaseLabel(name)}",
                $"Own wind   Vx {Num(lc?.Vx - lc?.AboveVx, "0.0")}  Vy {Num(lc?.Vy - lc?.AboveVy, "0.0")} k  Mt {Num(lc?.Mt - lc?.AboveMt, "0")} k·ft",
                above.Count > 0
                    ? $"From {upperName} (orange)   Vx {Num(lc?.AboveVx, "0.0")}  Vy {Num(lc?.AboveVy, "0.0")} k  Mt {Num(lc?.AboveMt, "0")} k·ft"
                    : $"Nothing from above{(currentLevel + 1 < stories.Count ? "" : " (roof level)")}",
                $"Diaphragm   Vx {Num(lc?.Vx, "0.0")}  Vy {Num(lc?.Vy, "0.0")} k" + (row?.MtComb != null ? $"   Mt,comb {Num(row.MtComb, "0")} k·ft" : ""),
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
            // Cases 3 and 4 (both directions at once) are the quartering wind: at 45° onto the windward corner. Cases 1 and 2
            // blow square onto the middle of the windward side.
            bool oblique = Math.Abs(d.X) > 1e-6 && Math.Abs(d.Y) > 1e-6;
            var mid = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
            var aim = oblique ? new Point(d.X > 0 ? box.Left : box.Right, d.Y > 0 ? box.Top : box.Bottom) : mid; // Rect.Top is the least y: the south side
            var wd = oblique ? new Vector(Math.Sign(d.X), Math.Sign(d.Y)) : d;
            var sd = WorldToScreen(aim + wd, m_layers) - WorldToScreen(aim, m_layers);
            if (sd.Length < 1e-9) return;
            sd.Normalize();
            Point head;
            if (oblique) head = WorldToScreen(aim, m_layers) - sd * 16; // just off the corner
            else
            {
                // from the center back against the wind to the box edge, then clear of it
                double tx = Math.Abs(sd.X) > 1e-9 ? screenBox.Width / 2 / Math.Abs(sd.X) : double.MaxValue;
                double ty = Math.Abs(sd.Y) > 1e-9 ? screenBox.Height / 2 / Math.Abs(sd.Y) : double.MaxValue;
                head = c - sd * (Math.Min(tx, ty) + 28);
            }
            var tail = head - sd * 95;
            // kept on the canvas (the building may fill the view): first shortened, keeping the head off the building and its
            // tags; only when even a short arrow will not fit is it slid toward the building
            var inner = new Rect(40, 40, Math.Max(1, m_layers.ActualWidth - 80), Math.Max(1, m_layers.ActualHeight - 80));
            for (int k = 0; k < 200 && !inner.Contains(tail) && (head - tail).Length > 44; k++) tail += sd * 4;
            for (int k = 0; k < 200 && !inner.Contains(tail); k++) { head += sd * 4; tail += sd * 4; }
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
            var label = new FormattedText($"WIND from {CaseBreakdown.WindFrom(lc)}{(oblique ? " (quartering, on the corner)" : "")}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                          new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 13 * LabelScale, WindInk, dip);
            var at = tail - sd * 8 - new Vector(label.Width / 2, label.Height / 2) + new Vector(-sd.X * label.Width / 2, -sd.Y * label.Height / 2);
            at = new Point(Math.Max(4, Math.Min(at.X, m_layers.ActualWidth - label.Width - 8)), Math.Max(4, Math.Min(at.Y, m_layers.ActualHeight - label.Height - 4)));
            ctx.DrawRectangle(LabelBack, null, new Rect(at.X - 3, at.Y, label.Width + 6, label.Height));
            ctx.DrawText(label, at);
            // the legend and the labels keep off the arrow and its label
            var arrow = new Rect(tail, head);
            arrow.Inflate(14, 14);
            labelsTaken.Add(arrow);
            windRect = Rect.Union(arrow, new Rect(at.X - 3, at.Y, label.Width + 6, label.Height));
            labelsTaken.Add(new Rect(at.X - 3, at.Y, label.Width + 6, label.Height));

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
            Label(ctx, $"Mt {Math.Abs(mt):0.0} k·ft {(ccwPlan ? "CCW" : "CW")}", new Point(c.X + r + 6, c.Y - r + 4), WindInk, dip, above: true);
        }

        /// <summary>
        /// An arrow of <paramref name="px"/> pixels centered on a plan point, along a plan direction, set off the line by
        /// <paramref name="side"/> px. When the arrow or its label would land on a tag, a label or the schedule, it slides along
        /// its line (up to <paramref name="slide"/> px either way) to the first clear place, then reserves where it went.
        /// </summary>
        private void Arrow(DrawingContext ctx, Point at, Vector dir, double px, double side, Pen pen, Brush ink, string label, double dip, double slide = 60)
        {
            var s0 = WorldToScreen(at, m_layers);
            var sd = WorldToScreen(at + dir, m_layers) - s0;
            if (sd.Length < 1e-9) return;
            sd.Normalize();
            var perp = new Vector(-sd.Y, sd.X);
            var ft = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10.5 * LabelScale, ink, dip);
            Rect ArrowBox(Point p) { var r = new Rect(p - sd * px / 2, p + sd * px / 2); r.Inflate(5, 5); return r; }
            Rect LabelBox(Point p)
            {
                var a = p + perp * (side > 0 ? 4 : -4) + new Vector(4, 0);
                if (side < 0) a.Y -= ft.Height;
                return new Rect(a.X - 2, a.Y, ft.Width + 4, ft.Height);
            }
            bool Clear(Point p) { var a = ArrowBox(p); var l = LabelBox(p); return !labelsTaken.Any(t => t.IntersectsWith(a) || t.IntersectsWith(l)); }

            var c0 = s0 + perp * side;
            var c = c0;
            double step = Math.Max(10, Math.Min(30, slide / 3));
            for (double d = step; d <= slide + 1e-9 && !Clear(c); d += step)
            {
                if (Clear(c0 + sd * d)) { c = c0 + sd * d; break; }
                if (Clear(c0 - sd * d)) { c = c0 - sd * d; break; }
            }
            labelsTaken.Add(ArrowBox(c));
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
            // on the arrow's side: the label grows away from the arrow (upward on the negative side)
            Label(ctx, label, c + perp * (side > 0 ? 4 : -4) + new Vector(4, 0), ink, dip, above: side < 0);
        }

        /// <summary>The Loads legend lines this frame (null when Loads is off): shown in the drawing key beside the plan.</summary>
        private IList<string> loadsKey;

        /// <summary>The overlay's legend: kept for the drawing key beside the plan, not drawn over it.</summary>
        private void Legend(DrawingContext ctx, IList<string> lines, double dip) => loadsKey = lines;

        private string drawingKeyShown;

        /// <summary>
        /// The drawing key under the tabs, right of the plan: the shear wall schedule (SHOW Design) and the Loads legend
        /// (SHOW Loads), so neither covers the plan; shown only while something is picked on the plan. Rebuilt only when
        /// what it says changes; called after each plan draw and when the selection changes.
        /// </summary>
        private void UpdateDrawingKey()
        {
            if (drawingKey == null) return;
            // only while something is picked on the plan: the schedule is on the Design tab and each case on the Calculations
            // tab already, so with nothing picked the key stays out of the way
            bool picked = Sketch?.Selection.Count > 0;
            var schedule = picked && showDesign && !foundationView && !InWindWorkspace && designResults != null && designResults.Count > 0
                ? ShearWallCalculator.Design.WallSchedule.Build(designResults, MarkLevel(currentLevel)) : new List<ShearWallCalculator.Design.ScheduleEntry>();
            var loads = InWindWorkspace || !picked ? null : loadsKey;
            string sig = string.Join("\n", schedule.Select(e => $"{e.Mark}|{e.Sheathing}|{e.Capacity}|{e.Holdown}|{string.Join(",", e.WallIds)}"))
                         + "\n#\n" + (loads == null ? "" : string.Join("\n", loads));
            if (sig == drawingKeyShown) return;
            drawingKeyShown = sig;
            // not during the plan's render: the layout change waits for it to finish
            Dispatcher.BeginInvoke(new Action(() => BuildDrawingKey(schedule, loads)));
        }

        private void BuildDrawingKey(List<ShearWallCalculator.Design.ScheduleEntry> schedule, IList<string> loads)
        {
            drawingKeyPanel.Children.Clear();
            TextBlock T(string text, Brush ink, double size = 12, bool bold = false) =>
                new TextBlock { Text = text, Foreground = ink, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap };

            if (schedule.Count > 0)
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 28, 6), MaxWidth = 520 };
                sp.Children.Add(T("SHEAR WALL SCHEDULE", Controls.Ui.InkMuted, 11, true));
                var g = new Grid { Margin = new Thickness(0, 2, 0, 0) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                foreach (var e in schedule)
                {
                    string walls = e.Holdown ?? "no hold-down"; // the mark names the wall; its hold-down completes its design
                    string cap = e.Capacity > 0 ? $"{e.Capacity:0} plf" : "no capacity";
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var m = T(e.Mark, Controls.Ui.Ink, 12, true); m.Margin = new Thickness(0, 1, 10, 1);
                    var t = T($"{e.Sheathing}  ·  {cap}  ·  {walls}", Controls.Ui.InkSecondary); t.Margin = new Thickness(0, 1, 0, 1);
                    Grid.SetRow(m, g.RowDefinitions.Count - 1); Grid.SetRow(t, g.RowDefinitions.Count - 1); Grid.SetColumn(t, 1);
                    g.Children.Add(m); g.Children.Add(t);
                }
                sp.Children.Add(g);
                var hd = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
                hd.Children.Add(new Border { Width = 8, Height = 8, Background = Controls.Ui.Ink, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
                hd.Children.Add(T("hold-down at the post centerline (both ends)", Controls.Ui.InkMuted, 11));
                sp.Children.Add(hd);
                drawingKeyPanel.Children.Add(sp);
            }
            if (loads != null && loads.Count > 0)
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6), MaxWidth = 520 };
                sp.Children.Add(T(loads[0].ToUpperInvariant(), Controls.Ui.InkMuted, 11, true));
                foreach (var line in loads.Skip(1))
                {
                    var ink = line.Contains("(orange)") ? AboveInk : Controls.Ui.InkSecondary;
                    var t = T(line, ink); t.Margin = new Thickness(0, 1, 0, 1);
                    sp.Children.Add(t);
                }
                if (loads.Count > 1) { var t = T("Blue: what the top of each wall here receives", TopInk); t.Margin = new Thickness(0, 1, 0, 1); sp.Children.Add(t); }
                drawingKeyPanel.Children.Add(sp);
            }
            bool any = drawingKeyPanel.Children.Count > 0;
            drawingKey.Background = Controls.Ui.Surface;
            drawingKey.BorderBrush = Controls.Ui.Hairline;
            drawingKey.Visibility = any && dataOpen ? Visibility.Visible : Visibility.Collapsed; // not while DATA is closed
            // the tabs end above the key
            drawingKey.UpdateLayout();
            MainTabControl.Margin = new Thickness(2, 0, 0, any ? drawingKey.ActualHeight + 6 : 0);
        }

    }
}
