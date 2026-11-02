using ASCE7WindLoadCalculator;
using BuildingModel.Diagnostics;
using BuildingModel.Model;
using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The detail of a wall, a beam or a column: its drawing (a wall in elevation with its hold-downs and the forces from
    /// above; a beam with its loads and reactions; a column with what lands on it and its base) and its calculation, in a
    /// window that follows the selection and the design. The same drawings and calculations go into the report.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>A beam or column selected on the plan (<see cref="selectedKind"/> "Beam" or "Column").</summary>
        private string selectedModelId;

        private MemberDetailWindow detailWindow;
        private (string kind, int story, int key, string modelId) detailSubject;

        /// <summary>The beams and columns of the model, by story (for the load path and the details).</summary>
        private (List<FramingBeam> beams, List<FramingColumn> columns) Framing()
        {
            var beams = new List<FramingBeam>();
            var columns = new List<FramingColumn>();
            if (building == null || stories.Count == 0) return (beams, columns);
            int StoryOf(string levelId) => stories.FindIndex(s => s.Level.Id == levelId);
            foreach (var bm in building.Beams)
            {
                int i = StoryOf(bm.LevelId);
                if (i >= 0) beams.Add(new FramingBeam { Id = bm.Id, Start = new Point(bm.Start.X, bm.Start.Y), End = new Point(bm.End.X, bm.End.Y), OnStory = i });
            }
            foreach (var c in building.Columns)
            {
                int top = StoryOf(c.TopLevelId);
                int below = StoryOf(c.BaseLevelId);
                int bas = building.Ground != null && c.BaseLevelId == building.Ground.Id ? 0 : below < 0 ? -1 : below + 1;
                if (top >= 0 && bas >= 0 && bas <= top) columns.Add(new FramingColumn { Id = c.Id, At = new Point(c.At.X, c.At.Y), BaseStory = bas, TopStory = top });
            }
            return (beams, columns);
        }

        /// <summary>
        /// The Walls &amp; diaphragms tab's beams (in the framing of the level shown) and columns (standing under it), with
        /// what each carries in the load path.
        /// </summary>
        private void ShowFramingRows()
        {
            if (geometryPanel == null) return;
            var beams = new List<Controls.GeometryPanel.FramingRow>();
            var columns = new List<Controls.GeometryPanel.FramingRow>();
            if (building != null && currentLevel >= 0 && currentLevel < stories.Count && !foundationView)
            {
                var (fb, fc) = Framing();
                var landings = AllLandings();
                foreach (var b in fb.Where(x => x.OnStory == currentLevel).OrderBy(x => x.Id))
                {
                    var r = loadPath?.Beams.FirstOrDefault(x => x.Beam.Id == b.Id);
                    beams.Add(new Controls.GeometryPanel.FramingRow
                    {
                        Id = b.Id, Start = b.Start, End = b.End, Length = (b.End - b.Start).Length,
                        Carries = r == null || r.Loads.Count == 0 ? "nothing"
                                : $"{r.Loads.Count} hold-down{(r.Loads.Count == 1 ? "" : "s")}; ends {r.StartAsd:N0} / {r.EndAsd:N0} lb",
                    });
                }
                foreach (var c in fc.Where(x => x.BaseStory <= currentLevel && x.TopStory >= currentLevel).OrderBy(x => x.Id))
                {
                    var col = building.Find(c.Id) as Column;
                    var lo = col == null ? null : building.Level(col.BaseLevelId);
                    var hi = col == null ? null : building.Level(col.TopLevelId);
                    var on = landings.Where(l => l.Support == SupportKind.Column && l.SupportId == $"column {c.Id}").ToList();
                    columns.Add(new Controls.GeometryPanel.FramingRow
                    {
                        Id = c.Id, Start = c.At, End = c.At,
                        Span = $"{(lo == null || lo.Elevation == 0 ? "foundation" : lo.Name)} to {hi?.Name}" + (col?.BaseFixed == true ? " (set)" : ""),
                        Carries = on.Count == 0 ? "nothing" : $"{on.Count} load{(on.Count == 1 ? "" : "s")}, up to {on.Max(l => l.TensionAsd):N0} lb",
                    });
                }
            }
            geometryPanel.ShowFraming(beams, columns, selectedKind == "Beam" || selectedKind == "Column" ? selectedModelId : null);
        }

        /// <summary>The selection card's Detail, or a beam or column picked: the detail window shows it.</summary>
        private void OpenDetail(string kind, int story, int key, string modelId)
        {
            detailSubject = (kind, story, key, modelId);
            if (detailWindow == null)
            {
                detailWindow = new MemberDetailWindow { Owner = this };
                detailWindow.Closed += (s, e) => detailWindow = null;
            }
            detailWindow.Show(DetailFor(detailSubject));
            detailWindow.Show();
            detailWindow.Activate();
            Log.Info("Detail", $"Detail of {kind} {modelId ?? key.ToString()} opened.");
        }

        /// <summary>After the design (and a new selection): the open detail follows the selected wall, beam or column.</summary>
        private void RefreshDetail()
        {
            if (detailWindow == null) return;
            if (selectedKind == "Wall" && selectedId >= 0) detailSubject = ("Wall", Math.Max(0, currentLevel), selectedId, null);
            else if ((selectedKind == "Beam" || selectedKind == "Column") && selectedModelId != null) detailSubject = (selectedKind, -1, -1, selectedModelId);
            detailWindow.Show(DetailFor(detailSubject));
        }

        private List<HoldownLanding> AllLandings() =>
            landingsOnto.Values.SelectMany(x => x).Concat(foundationAnchors).ToList();

        /// <summary>A wall's own hold-downs as traced: where each anchors.</summary>
        private List<HoldownLanding> OwnLandings(int story, int key)
        {
            var at = story == 0 ? foundationAnchors : landingsOnto.TryGetValue(story - 1, out var l) ? l : new List<HoldownLanding>();
            return at.Where(x => x.Kind == PathPointKind.WallHoldown && x.UpperStory == story + 1 && x.WallId == key).ToList();
        }

        private DetailContent DetailFor((string kind, int story, int key, string modelId) s)
        {
            switch (s.kind)
            {
                case "Wall": return WallDetail(s.story, s.key);
                case "Beam": return BeamDetail(s.modelId);
                case "Column": return ColumnDetail(s.modelId);
                default: return null;
            }
        }

        private DetailContent WallDetail(int i, int key)
        {
            var calcs = levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator };
            if (i < 0 || i >= calcs.Count || calcs[i]?._wall_system == null || !calcs[i]._wall_system._walls.TryGetValue(key, out var w)) return null;
            string name = i < stories.Count && stories[i].Walls.TryGetValue(key, out var id) ? id : $"Wall {key}";
            var design = i < allDesignResults.Count ? allDesignResults[i] : designResults;
            bool ew = w.WallDir == WallDirs.EastWest;
            var c = new DetailContent
            {
                Title = $"{name} · {LevelName(i)}",
                Subtitle = $"{(ew ? "East-west" : "North-south")} wall, {w.WallLength:0.##} ft long, {w.WallHeight:0.##} ft high (story {i + 1}). " +
                           $"Elevation with the {(ew ? "west" : "south")} end on the left. Loads at ASD (0.6 W) for the hold-downs.",
                Footnote = ShearWallDesigner.CatalogFootnote,
            };
            if (design == null || !design.TryGetValue(key, out var r))
            {
                c.Lines.Add(new CalcLine("Design", "not calculated yet: the wall has no shear (loads, a diaphragm, walls both ways)", ""));
                return c;
            }
            var own = OwnLandings(i, key);
            c.Drawing = () => new WallElevation(w, r, own);
            c.Lines = DetailCalcs.Wall(w, r, own);
            return c;
        }

        private DetailContent BeamDetail(string id)
        {
            var bm = building?.Find(id) as Beam;
            if (bm == null) return null;
            var br = loadPath?.Beams.FirstOrDefault(x => x.Beam.Id == id)
                     ?? new BeamReactions { Beam = new FramingBeam { Id = id, Start = new Point(bm.Start.X, bm.Start.Y), End = new Point(bm.End.X, bm.End.Y) } };
            var ends = AllLandings().Where(l => l.Kind == PathPointKind.BeamEnd && l.From.StartsWith($"beam {id} ")).ToList();
            var level = building.Level(bm.LevelId);
            var c = new DetailContent
            {
                Title = $"Beam {id} · {level?.Name}",
                Subtitle = $"In the framing of {level?.Name}, {bm.Length:0.##} ft. " +
                           (br.Loads.Count == 0 ? "It carries no hold-downs." : $"It carries {br.Loads.Count} hold-down{(br.Loads.Count == 1 ? "" : "s")} from the story above to its ends (simple span)."),
                Footnote = "The beam itself is not sized here: these are the uplift reactions its end connections and supports must take.",
            };
            c.Drawing = () => new BeamDiagram(br, ends);
            c.Lines = DetailCalcs.Beam(br, ends);
            return c;
        }

        private DetailContent ColumnDetail(string id)
        {
            var col = building?.Find(id) as Column;
            if (col == null) return null;
            var lo = building.Level(col.BaseLevelId);
            var hi = building.Level(col.TopLevelId);
            var fc = Framing().columns.FirstOrDefault(x => x.Id == id) ?? new FramingColumn { Id = id, At = new Point(col.At.X, col.At.Y) };
            var all = AllLandings();
            var onIt = all.Where(l => l.Support == SupportKind.Column && l.SupportId == $"column {id}").ToList();
            var bottom = all.FirstOrDefault(l => l.Kind == PathPointKind.ColumnBase && l.From == $"column {id} base");
            string span = $"from {(lo?.Elevation == 0 ? "the foundation" : lo?.Name)} up to {hi?.Name}" + (col.BaseFixed ? " (base set by hand)" : "");
            var c = new DetailContent
            {
                Title = $"Column {id}",
                Subtitle = $"At ({col.At.X:0.##}, {col.At.Y:0.##}), {span}. It carries what lands on its top down to its base.",
                Footnote = "The column itself is not sized here: this is the uplift its connections must take.",
            };
            var levelList = building.LevelsBottomUp.Select(l => (l.Name, l.Elevation)).ToList();
            c.Drawing = () => new ColumnDiagram(id, lo?.Elevation ?? 0, hi?.Elevation ?? 0, levelList, onIt, bottom);
            c.Lines = DetailCalcs.Column(fc, span, onIt, bottom);
            return c;
        }

        /// <summary>The selection card for a beam or a column: what it carries, its reactions, and Detail.</summary>
        private void ShowMemberCard()
        {
            var o = building?.Find(selectedModelId);
            var rows = new List<(string, string, bool)>();
            var all = AllLandings();
            if (o is Beam bm)
            {
                var br = loadPath?.Beams.FirstOrDefault(x => x.Beam.Id == bm.Id);
                var ends = all.Where(l => l.Kind == PathPointKind.BeamEnd && l.From.StartsWith($"beam {bm.Id} ")).ToList();
                rows.Add(("Carries", br == null ? "no hold-downs" : $"{br.Loads.Count} hold-down{(br.Loads.Count == 1 ? "" : "s")}", false));
                if (br != null) rows.Add(("Reactions", $"{br.StartAsd:N0} / {br.EndAsd:N0} lb up (ASD)", false));
                foreach (var e in ends)
                    rows.Add((e.From.EndsWith("start") ? "Start" : "End", e.Supported ? $"{DetailCalcs.Support(e)}: {e.Model ?? "none strong enough"}" : "nothing under it", !e.Supported || e.Model == null));
                selectionCard.ShowMember($"Beam {bm.Id}", $"{building.Level(bm.LevelId)?.Name} · {bm.Length:0.##} ft", rows,
                    () => OpenDetail("Beam", -1, -1, bm.Id), () => Sketch.Edit($"Deleted {bm.Id}.  Ctrl+Z brings it back.", b => BuildingModel.Edits.BuildingEdits.Delete(b, bm.Id)));
            }
            else if (o is Column col)
            {
                var lo = building.Level(col.BaseLevelId);
                var onIt = all.Where(l => l.Support == SupportKind.Column && l.SupportId == $"column {col.Id}").ToList();
                var bottom = all.FirstOrDefault(l => l.Kind == PathPointKind.ColumnBase && l.From == $"column {col.Id} base");
                rows.Add(("Stands on", lo?.Elevation == 0 ? "the foundation" : $"{BuildingModel.Edits.BuildingEdits.SupportAt(building, lo, col.At, col.Id) ?? "nothing"} at {lo?.Name}" + (col.BaseFixed ? " (set by hand)" : ""), false));
                rows.Add(("Carries", onIt.Count == 0 ? "nothing" : $"{onIt.Sum(l => l.TensionAsd):N0} lb up from {onIt.Count} point{(onIt.Count == 1 ? "" : "s")}", false));
                if (bottom != null) rows.Add(("Base", bottom.Supported ? $"{DetailCalcs.FamilyName(bottom.Family)} {bottom.Model ?? "none strong enough"}" : "nothing under it", !bottom.Supported || bottom.Model == null));
                selectionCard.ShowMember($"Column {col.Id}", $"up to {building.Level(col.TopLevelId)?.Name}", rows,
                    () => OpenDetail("Column", -1, -1, col.Id), () => Sketch.Edit($"Deleted {col.Id}.  Ctrl+Z brings it back.", b => BuildingModel.Edits.BuildingEdits.Delete(b, col.Id)));
            }
            else selectionCard.Show(Calculator, null, -1, designResults);
        }

        // ------------------------------------------------------------------ the report

        /// <summary>
        /// The report's detail pages: each wall that needs hold-downs (or carries forces from above), story by story from the top,
        /// in elevation with its calculation; then the beams and columns that carry hold-downs.
        /// </summary>
        private IEnumerable<ReportSection> DetailReportSections()
        {
            var sections = new List<ReportSection>();
            if (allDesignResults.Count == 0) return sections;
            var calcs = levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator };
            for (int i = calcs.Count - 1; i >= 0; i--)
            {
                if (i >= allDesignResults.Count || calcs[i]?._wall_system == null) continue;
                var blocks = new List<FrameworkElement>();
                foreach (var kv in allDesignResults[i].OrderBy(k => k.Key))
                {
                    if (!kv.Value.HoldownNeeded && (kv.Value.FromAbove == null || kv.Value.FromAbove.Count == 0)) continue;
                    var d = WallDetail(i, kv.Key);
                    if (d == null) continue;
                    blocks.Add(WindReport.Heading(d.Title));
                    blocks.Add(WindReport.Para(d.Subtitle));
                    if (d.Drawing != null) blocks.Add(d.Drawing());
                    blocks.AddRange(WindReport.Tables(new[] { "Item", "Calculation", "Result" }, d.Lines.Select(l => new[] { l.Item, l.Calculation, l.Result }).ToList()));
                }
                if (blocks.Count > 0) sections.Add(new ReportSection($"{LevelName(i)}: wall details", blocks));
            }
            var members = new List<FrameworkElement>();
            foreach (var br in loadPath?.Beams ?? new List<BeamReactions>())
            {
                var d = BeamDetail(br.Beam.Id);
                if (d == null) continue;
                members.Add(WindReport.Heading(d.Title));
                members.Add(WindReport.Para(d.Subtitle));
                members.Add(d.Drawing());
                members.AddRange(WindReport.Tables(new[] { "Item", "Calculation", "Result" }, d.Lines.Select(l => new[] { l.Item, l.Calculation, l.Result }).ToList()));
            }
            foreach (var col in building?.Columns ?? new List<Column>())
            {
                var d = ColumnDetail(col.Id);
                if (d == null || !AllLandings().Any(l => l.SupportId == $"column {col.Id}")) continue;
                members.Add(WindReport.Heading(d.Title));
                members.Add(WindReport.Para(d.Subtitle));
                members.Add(d.Drawing());
                members.AddRange(WindReport.Tables(new[] { "Item", "Calculation", "Result" }, d.Lines.Select(l => new[] { l.Item, l.Calculation, l.Result }).ToList()));
            }
            if (members.Count > 0) sections.Add(new ReportSection("Beams and columns carrying hold-downs", members));
            return sections;
        }
    }
}
