using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>
    /// The edits of a wall's design inputs offered in the Design table and on the plan's wall card, in one place so both
    /// make the same change: the face 1 sheathing and its edge nailing, a second face (none, the same as face 1, or its
    /// own sheathing), and the hold-down type.
    /// </summary>
    public static class WallDesignEdits
    {
        /// <summary>The first edge spacing (6, 4, 3, 2") the option has a wind value for; 6 when none.</summary>
        public static int FirstSpacing(SheathingOption o) =>
            ShearWallDesigner.EdgeSpacings.Where(s => !double.IsNaN(o.NominalWind(s))).DefaultIfEmpty(6).First();

        /// <summary>A face of <paramref name="o"/>: keeps <paramref name="spacing"/> when the table has a value for it, else its first spacing; 0 for a Table 4.3C row.</summary>
        public static FaceSheathing Face(SheathingOption o, int spacing)
        {
            if (!o.UsesEdgeSpacing) return o.ToFace(0);
            if (spacing <= 0 || double.IsNaN(o.NominalWind(spacing))) spacing = FirstSpacing(o);
            return o.ToFace(spacing);
        }

        /// <summary>True when the second face is the same construction as the first (SetFace1 and the spacing then change both).</summary>
        public static bool SecondIsSame(WallDesignInputs d)
        {
            var f2 = d.EffectiveFace2();
            return f2 != null && d.EffectiveFace1().SameConstructionAs(f2);
        }

        /// <summary>Face 1 to <paramref name="o"/>, keeping its edge spacing where the table allows; a "same" face 2 follows.</summary>
        public static void SetFace1(WallDesignInputs d, SheathingOption o)
        {
            var f1 = d.EffectiveFace1();
            bool same = SecondIsSame(d);
            var n = Face(o, f1.EdgeSpacing > 0 ? f1.EdgeSpacing : 6);
            d.SetFaces(n, same ? n : d.EffectiveFace2());
        }

        /// <summary>Face 1's edge spacing (in.); a "same" face 2 follows.</summary>
        public static void SetFace1Spacing(WallDesignInputs d, int spacing)
        {
            bool same = SecondIsSame(d);
            var n = d.EffectiveFace1().Clone();
            n.EdgeSpacing = spacing;
            d.SetFaces(n, same ? n : d.EffectiveFace2());
        }

        /// <summary>One face only.</summary>
        public static void SetNoFace2(WallDesignInputs d) => d.SetFaces(d.EffectiveFace1(), null);

        /// <summary>Face 2 the same construction as face 1.</summary>
        public static void SetFace2Same(WallDesignInputs d) { var f1 = d.EffectiveFace1(); d.SetFaces(f1, f1.Clone()); }

        /// <summary>Face 2 its own sheathing (6" edges where the table has them).</summary>
        public static void SetFace2(WallDesignInputs d, SheathingOption o) => d.SetFaces(d.EffectiveFace1(), Face(o, 6));

        /// <summary>Face 2's edge spacing (in.), when it is its own sheathing.</summary>
        public static void SetFace2Spacing(WallDesignInputs d, int spacing)
        {
            var f2 = d.EffectiveFace2();
            if (f2 == null) return;
            var n = f2.Clone();
            n.EdgeSpacing = spacing;
            d.SetFaces(d.EffectiveFace1(), n);
        }

        /// <summary>The hold-down type; a model chosen for the old type means nothing for the new one, so the size goes back to Auto.</summary>
        public static void SetHoldownType(WallDesignInputs d, HoldownFamilies f)
        {
            if (d.Holdown == f) return;
            d.Holdown = f;
            d.HoldownKey = null;
        }
    }
}
