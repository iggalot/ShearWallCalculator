using ASCE7WindLoadCalculator;
using BuildingModel.Diagnostics;
using BuildingModel.Model;
using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.ModelAdapter;
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

        /// <summary>The selected wall's construction window (elevation, 3D, pieces, calculation); null while closed.</summary>
        private Dialogs.WallBuildWindow wallBuild;
        /// <summary>Open the construction window whenever a wall is picked (its own check box; kept for the session).</summary>
        private bool wallBuildOnPick = true;

        /// <summary>
        /// The construction window follows the selected wall and the design. A wall picked opens it (unless its "Open when a
        /// wall is picked" is off); <paramref name="open"/>: the card's Detail, which opens it anyway.
        /// </summary>
        private void RefreshWallBuild(bool open = false)
        {
            bool wall = selectedKind == "Wall" && selectedId >= 0 && !foundationView;
            bool member = (selectedKind == "Beam" || selectedKind == "Column") && selectedModelId != null;
            if (wallBuild == null)
            {
                if (!(wall || member) || !(open || (wallBuildOnPick && selectionCardOpen))) return;
                wallBuild = new Dialogs.WallBuildWindow { Owner = this, OpenOnPick = wallBuildOnPick };
                wallBuild.Closed += (s, e) => { wallBuildOnPick = wallBuild?.OpenOnPick ?? wallBuildOnPick; wallBuild = null; };
                wallBuild.UndoRedo += redo => { if (redo) Sketch.Redo(); else Sketch.Undo(); };
                // a stud size picked there is the wall's, in the building model (every app reads it): an edit through the
                // sketch, so it is saved, undoable and logged like any other model edit
                wallBuild.StudSizeChosen += size =>
                {
                    var d = wallBuild?.Data;
                    if (selectedKind != "Wall" || building == null || d?.ModelWallId == null || d.TopLevelId == null) return;
                    var mw = building.Find(d.ModelWallId) as Wall;
                    if (mw == null || (Building.OwnFraming(mw, d.TopLevelId)?.StudSize ?? null) == size) return;
                    double spacing = Building.OwnFraming(mw, d.TopLevelId)?.StudSpacing ?? 0;
                    string what = $"{d.Name}: studs {size ?? $"{d.DefaultStudSize} (project default)"}.";
                    Dispatcher.BeginInvoke(new Action(() =>
                        Sketch.Edit(what, b => BuildingModel.Edits.BuildingEdits.SetStudFraming(b, d.ModelWallId, d.TopLevelId, size, spacing))),
                        System.Windows.Threading.DispatcherPriority.Input);
                };
                wallBuild.Show();
                Log.Info("Construction", $"Construction window opened for {selectedKind} {selectedModelId ?? selectedId.ToString()}.");
            }
            else wallBuildOnPick = wallBuild.OpenOnPick;
            if (wall) wallBuild.Show(WallBuildFor(Math.Max(0, currentLevel), selectedId));
            else if (member) wallBuild.ShowMember(selectedKind == "Beam" ? BeamViewFor(selectedModelId) : ColumnViewFor(selectedModelId));
            if (open) wallBuild.Activate();
        }

        /// <summary>A beam's construction view: its span, the hold-downs on it, each end's reaction, connection and support.</summary>
        private Dialogs.MemberView BeamViewFor(string id)
        {
            var bm = building?.Find(id) as Beam;
            if (bm == null) return null;
            var br = loadPath?.Beams.FirstOrDefault(x => x.Beam.Id == id)
                     ?? new BeamReactions { Beam = new FramingBeam { Id = id, Start = new Point(bm.Start.X, bm.Start.Y), End = new Point(bm.End.X, bm.End.Y) } };
            var landings = AllLandings();
            var cols = Framing().columns;
            Dialogs.BeamEndInfo End(string which, Point at, double asd)
            {
                var l = landings.FirstOrDefault(x => x.Kind == PathPointKind.BeamEnd && x.From == $"beam {id} {which}");
                // what it bears on: as traced; with no uplift (no landing), a column at the end, if there is one
                string under = l != null ? DetailCalcs.Support(l)
                             : cols.FirstOrDefault(fc => (fc.At - at).Length <= 0.5) is FramingColumn under0 ? $"column {under0.Id}" : "a wall or beam (no uplift here)";
                return new Dialogs.BeamEndInfo { Which = which, Reaction = asd, Landing = l, Under = under };
            }
            var level = building.Level(bm.LevelId);
            var ends = landings.Where(l => l.Kind == PathPointKind.BeamEnd && l.From.StartsWith($"beam {id} ")).ToList();
            var lines = DetailCalcs.Beam(br, ends) ?? new List<CalcLine>();
            var view = Dialogs.MemberViews.Beam(id, level?.Name, br, End("start", br.Beam.Start, br.StartAsd), End("end", br.Beam.End, br.EndAsd),
                                            lines, "The beam itself is not sized here: these are the uplift reactions its end connections and supports must take.");
            // the building model's north arrow (as in every view): the beam's own axes run along it from its start, seen from its right
            Point s = br.Beam.Start, e = br.Beam.End;
            view.North = BuildingModel.Model.NorthArrow.For(new BuildingModel.Geometry.Rect2(Math.Min(s.X, e.X), Math.Min(s.Y, e.Y), Math.Max(s.X, e.X), Math.Max(s.Y, e.Y)), Dialogs.MemberViews.BeamDepth / 12);
            view.FromPlan = WoodFraming.Wpf.FramingModel.FromPlan(s.X, s.Y, e.X, e.Y, Dialogs.MemberViews.BeamWidth);
            return view;
        }

        /// <summary>A column's construction view: the levels it passes, what lands on its top, its base connection.</summary>
        private Dialogs.MemberView ColumnViewFor(string id)
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
            var levelList = building.LevelsBottomUp.Select(l => (l.Name, l.Elevation)).ToList();
            var lines = DetailCalcs.Column(fc, span, onIt, bottom) ?? new List<CalcLine>();
            var view = Dialogs.MemberViews.Column(id, lo?.Elevation ?? 0, hi?.Elevation ?? 0, levelList, onIt, bottom, span,
                                              lines, "The column itself is not sized here: this is the uplift its connections must take.");
            // the building model's north arrow (as in every view): the post's own axes x east from its west face, z south
            double half = Dialogs.MemberViews.PostSize / 24;
            view.North = BuildingModel.Model.NorthArrow.For(new BuildingModel.Geometry.Rect2(col.At.X - half, col.At.Y - half, col.At.X + half, col.At.Y + half), Math.Max(0, (hi?.Elevation ?? 0) - (lo?.Elevation ?? 0)));
            view.FromPlan = WoodFraming.Wpf.FramingModel.FromPlan(col.At.X - half, col.At.Y, col.At.X + half, col.At.Y, Dialogs.MemberViews.PostSize);
            return view;
        }

        /// <summary>What the construction window shows for wall <paramref name="key"/> of story <paramref name="i"/>.</summary>
        private Dialogs.WallBuildData WallBuildFor(int i, int key)
        {
            var calcs = levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator };
            if (i < 0 || i >= calcs.Count || calcs[i]?._wall_system == null || !calcs[i]._wall_system._walls.TryGetValue(key, out var w)) return null;
            var design = i < allDesignResults.Count ? allDesignResults[i] : designResults;
            WallDesignResult r = null;
            design?.TryGetValue(key, out r);
            // its schedule mark (SW2-11, as on the plan's tag), with the model's name: "SW2-11 (W11)"
            string modelName = i < stories.Count && stories[i].Walls.TryGetValue(key, out var id) ? id : $"Wall {key}";
            string mark = design != null && design.Count > 0 && WallSchedule.MarksByWall(WallSchedule.Build(design, MarkLevel(i))).TryGetValue(key, out var m) ? m : null;
            var d = new Dialogs.WallBuildData
            {
                Name = mark != null ? $"{mark} ({modelName})" : modelName,
                LevelName = LevelName(i), Story = i, Stories = Math.Max(1, calcs.Count), Wall = w, Result = r,
                Own = OwnLandings(i, key), Footnote = ShearWallDesigner.CatalogFootnote,
            };
            if (r != null) d.Lines = DetailCalcs.Wall(w, r, d.Own);
            // its studs, from the building model: the wall's own size and spacing on this story, else the project's
            string modelId = i < stories.Count && stories[i].Walls.TryGetValue(key, out var mid) ? mid : null;
            var mw = modelId != null ? building?.Find(modelId) as Wall : null;
            string top = i < stories.Count ? stories[i].Level?.Id : null;
            var defaults = building?.Defaults ?? new ModelDefaults();
            d.ModelWallId = mw?.Id;
            d.TopLevelId = top;
            d.Defaults = defaults;
            bool first = building != null && top != null ? building.IsFirstStory(top) : d.FirstStory;
            d.DefaultStudSize = first ? defaults.StudSizeFirstStory : defaults.StudSizeAbove;
            string size = mw != null && top != null ? building.StudSizeOf(mw, top) : d.DefaultStudSize;
            d.OwnStudSize = mw != null && !string.IsNullOrEmpty(Building.OwnFraming(mw, top)?.StudSize);
            // a size the program cannot draw (a hand-edited file): the smallest it can, said in the log
            if (!WoodFraming.WallFraming.IsStudSize(size))
            {
                Log.Warning("Construction", $"{d.Name}: stud size \"{size}\" is not one of {string.Join(", ", WoodFraming.WallFraming.StudSizes)}; drawn as {WoodFraming.WallFraming.StudSizes[0]}.", null);
                size = WoodFraming.WallFraming.StudSizes[0];
            }
            d.StudSize = size;
            d.StudSpacing = mw != null && top != null ? building.StudSpacingOf(mw, top) : defaults.StudSpacing;
            // the wall view every app shows (WoodFraming.Wpf): the whole wall from the model, with this app's live design
            if (mw != null)
            {
                // (the design this window shows, linked to the wind or not; the foundation takes it only when linked)
                var live = ShearWallCheck.Lateral(levels, stories, allDesignResults, foundationAnchors, 0, loadPath?.ByLevel, building?.Ground?.Id, building?.Defaults) ?? BuildingModel.Model.LateralSection.Read(building);
                d.View = WoodFraming.Wpf.WallViews.FromModel(building, mw, top, live);
                if (d.View != null) d.View.Name = d.Name;
            }
            // what the story above stands on this wall: its hold-downs (and beam ends or column bases) landing on it
            string UpperWall(HoldownLanding x)
            {
                int s = x.UpperStory - 1;
                var up = s >= 0 && s < allDesignResults.Count ? allDesignResults[s] : null;
                if (up != null && up.Count > 0 && WallSchedule.MarksByWall(WallSchedule.Build(up, MarkLevel(s))).TryGetValue(x.WallId, out var um)) return um; // SW3-10
                return s >= 0 && s < stories.Count && stories[s].Walls.TryGetValue(x.WallId, out var n) ? n : $"W{x.WallId}";
            }
            var onto = landingsOnto.TryGetValue(i, out var here) ? here : new List<HoldownLanding>();
            foreach (var x in onto.Where(x => x.LowerWallId == key))
                d.Above.Add(new Dialogs.LoadFromAbove
                {
                    Landing = x, Along = d.AlongIn(x.At),
                    From = x.Kind == PathPointKind.WallHoldown ? UpperWall(x) : ShearWallCalculator.Drawing.PlanLabels.ShortFrom(x.From),
                });
            return d;
        }

        /// <summary>The beams and columns of the model, by story (for the load path and the details).</summary>
        private (List<FramingBeam> beams, List<FramingColumn> columns) Framing() => ShearWallCheck.Framing(building, stories);

        /// <summary>
        /// The Members tab's beams (each in its level's framing) and columns (listed at their top level), every level, with
        /// what each carries in the load path.
        /// </summary>
        private void ShowFramingRows()
        {
            if (geometryPanel == null) return;
            var beams = new List<Controls.GeometryPanel.FramingRow>();
            var columns = new List<Controls.GeometryPanel.FramingRow>();
            if (building != null && stories.Count > 0)
            {
                var (fb, fc) = Framing();
                var landings = AllLandings();
                foreach (var b in fb.OrderBy(x => x.OnStory).ThenBy(x => x.Id))
                {
                    var r = loadPath?.Beams.FirstOrDefault(x => x.Beam.Id == b.Id);
                    var level = stories[b.OnStory].Level;
                    beams.Add(new Controls.GeometryPanel.FramingRow
                    {
                        Id = b.Id, Level = level.Name, Elevation = level.Elevation, FromStory = b.OnStory, ToStory = b.OnStory,
                        Start = b.Start, End = b.End, Length = (b.End - b.Start).Length,
                        Carries = r == null || r.Loads.Count == 0 ? "nothing"
                                : $"{r.Loads.Count} hold-down{(r.Loads.Count == 1 ? "" : "s")}; " + EndsText(b.Id, r, landings),
                    });
                }
                foreach (var c in fc.OrderBy(x => x.TopStory).ThenBy(x => x.Id))
                {
                    var col = building.Find(c.Id) as Column;
                    var lo = col == null ? null : building.Level(col.BaseLevelId);
                    var hi = col == null ? null : building.Level(col.TopLevelId);
                    var on = landings.Where(l => l.Support == SupportKind.Column && l.SupportId == $"column {c.Id}").ToList();
                    columns.Add(new Controls.GeometryPanel.FramingRow
                    {
                        Id = c.Id, Level = stories[c.TopStory].Level.Name, Elevation = stories[c.TopStory].Level.Elevation,
                        FromStory = c.BaseStory, ToStory = c.TopStory, Start = c.At, End = c.At,
                        Span = $"{(lo == null || lo.Elevation == 0 ? "foundation" : lo.Name)} to {hi?.Name}" + (col?.BaseFixed == true ? " (set)" : ""),
                        Carries = on.Count == 0 ? "nothing" : $"{on.Count} load{(on.Count == 1 ? "" : "s")}, up to {on.Max(l => l.TensionAsd):N0} lb",
                    });
                }
            }
            geometryPanel.ShowFraming(beams, columns, selectedKind == "Beam" || selectedKind == "Column" ? selectedModelId : null);
        }

        /// <summary>Every story's walls and diaphragms for the Members tab (one unnamed set with no building model).</summary>
        private List<Controls.GeometryPanel.LevelSet> MemberLevels() =>
            stories.Count == 0
                ? new List<Controls.GeometryPanel.LevelSet> { new Controls.GeometryPanel.LevelSet { Story = 0, Name = "", Calculator = Calculator } }
                : stories.Select((s, i) => new Controls.GeometryPanel.LevelSet { Story = i, Name = s.Level.Name, Elevation = s.Level.Elevation, Calculator = s.Calculator }).ToList();

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

        /// <summary>A beam's end reactions and their connections: "start 367 lb → column C1 HRS6, end 420 lb → column C3 HRS6".</summary>
        private static string EndsText(string beamId, BeamReactions r, IEnumerable<HoldownLanding> landings)
        {
            string End(string which, double asd)
            {
                var l = landings.FirstOrDefault(x => x.Kind == PathPointKind.BeamEnd && x.From == $"beam {beamId} {which}");
                string to = l == null ? "" : !l.Supported ? " → nothing under it" : $" → {l.SupportId} {l.Model ?? "none strong enough"}";
                return $"{which} {asd:N0} lb{to}";
            }
            return End("start", r.StartAsd) + ", " + End("end", r.EndAsd);
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
            if (selectedKind == MultiWallKind) { ShowWallsCard(); return; }
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
                    () => RefreshWallBuild(open: true), null); // deleted in the Building Model
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
                    () => RefreshWallBuild(open: true), null); // deleted in the Building Model
            }
            else if (o is Wall pw)
            {
                var lo = building.Level(pw.BaseLevelId);
                var hi = building.Level(pw.TopLevelId);
                string dir = pw.Direction == WallDirection.WestEast ? "W-E" : pw.Direction == WallDirection.SouthNorth ? "S-N" : "angled";
                var plainRows = new List<(string, string)>
                {
                    ("Shear wall", "no: it carries its gravity load only"),
                    ("Height", $"{lo?.Name} → {hi?.Name}"),
                };
                selectionCard.ShowPlainWall($"Wall {pw.Id}", $"{dir} · {pw.Length:0.##} ft", plainRows,
                    pw.Direction == WallDirection.NotOrthogonal ? (Action)null : () => Later(() => MakeShearWall(pw.Id)));
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
