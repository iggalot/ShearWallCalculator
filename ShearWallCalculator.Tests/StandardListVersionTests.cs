using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.Interfaces;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The company default hold-down list shipped with the app, each user's copy of it, and the record a project keeps of the
    /// list and the hold-downs it was designed with.
    /// </summary>
    [TestClass]
    public class StandardListVersionTests
    {
        private string dir;
        private string UserPath => Path.Combine(dir, "user", "HoldownStandardList.json");
        private string CompanyPath => Path.Combine(dir, HoldownStandardList.PackagedFileName);

        [TestInitialize]
        public void Init() => dir = Path.Combine(Path.GetTempPath(), "swc-list-" + Guid.NewGuid().ToString("N"));

        [TestCleanup]
        public void Cleanup() { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

        private void WriteCompany(int version, params string[] keys)
        {
            var c = new HoldownStandardList();
            foreach (var k in keys) c.Set(k, true);
            var shipped = new HoldownStandardList();
            shipped.RestoreFrom(c);
            shipped.CompanyVersion = version;
            shipped.CompanyDate = "2026-11-0" + version;
            shipped.Save(CompanyPath);
        }

        [TestMethod]
        public void FirstRun_SeedsTheUserListFromTheCompanyDefault()
        {
            WriteCompany(1, "HDU5-SDS2.5#1", "HDU8-SDS2.5#1");
            var (user, company, update) = HoldownStandardList.Startup(UserPath, CompanyPath);
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.Seeded, update);
            Assert.IsTrue(user.SameKeysAs(company));
            Assert.AreEqual(1, user.CompanyVersion);
            Assert.IsFalse(user.Customized);
            Assert.IsTrue(File.Exists(UserPath), "the user's copy is written");
        }

        [TestMethod]
        public void NewerCompanyVersion_ReplacesAnUnchangedList()
        {
            WriteCompany(1, "HDU5-SDS2.5#1");
            HoldownStandardList.Startup(UserPath, CompanyPath);
            WriteCompany(2, "HDU5-SDS2.5#1", "HDU11-SDS2.5#1");

            var (user, _, update) = HoldownStandardList.Startup(UserPath, CompanyPath);
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.Updated, update);
            CollectionAssert.AreEquivalent(new[] { "HDU5-SDS2.5#1", "HDU11-SDS2.5#1" }, user.Keys.ToArray());
            Assert.AreEqual(2, HoldownStandardList.Load(UserPath).CompanyVersion, "saved");

            Assert.AreEqual(HoldownStandardList.CompanyUpdate.None, HoldownStandardList.Startup(UserPath, CompanyPath).update, "asked only once");
        }

        [TestMethod]
        public void NewerCompanyVersion_WithUserEdits_AsksAndKeepMineIsRemembered()
        {
            WriteCompany(1, "HDU5-SDS2.5#1");
            var first = HoldownStandardList.Startup(UserPath, CompanyPath).user;
            first.Set("LTTP2#1", true);
            Assert.IsTrue(first.Customized);
            first.Save(UserPath);
            WriteCompany(2, "HDU8-SDS2.5#1");

            var (user, company, update) = HoldownStandardList.Startup(UserPath, CompanyPath);
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.NeedsChoice, update);
            CollectionAssert.AreEquivalent(new[] { "HDU5-SDS2.5#1", "LTTP2#1" }, user.Keys.ToArray(), "nothing replaced before the user answers");

            user.KeepMineOver(company);
            user.Save(UserPath);
            var again = HoldownStandardList.Startup(UserPath, CompanyPath);
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.None, again.update, "not asked again for the same version");
            Assert.IsTrue(again.user.Customized);
            CollectionAssert.AreEquivalent(new[] { "HDU5-SDS2.5#1", "LTTP2#1" }, again.user.Keys.ToArray());

            again.user.RestoreFrom(again.company);
            CollectionAssert.AreEquivalent(new[] { "HDU8-SDS2.5#1" }, again.user.Keys.ToArray());
            Assert.IsFalse(again.user.Customized);
        }

        [TestMethod]
        public void UserEditsThatMatchTheNewVersion_AreUpdatedWithoutAsking()
        {
            WriteCompany(1, "HDU5-SDS2.5#1");
            var first = HoldownStandardList.Startup(UserPath, CompanyPath).user;
            first.Set("HDU8-SDS2.5#1", true);
            first.Save(UserPath);
            WriteCompany(2, "HDU5-SDS2.5#1", "HDU8-SDS2.5#1");
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.Updated, HoldownStandardList.Startup(UserPath, CompanyPath).update);
        }

        [TestMethod]
        public void ListSavedBeforeVersions_CountsAsTheUsersOwn()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(UserPath));
            File.WriteAllText(UserPath, "{ \"Keys\": [ \"HDU2-SDS2.5#1\" ] }");
            WriteCompany(1, "HDU5-SDS2.5#1");
            var (user, _, update) = HoldownStandardList.Startup(UserPath, CompanyPath);
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.NeedsChoice, update);
            Assert.IsTrue(user.Contains("HDU2-SDS2.5#1"));
        }

        [TestMethod]
        public void NoCompanyFile_IsAnEmptyVersionZero()
        {
            var (user, company, update) = HoldownStandardList.Startup(UserPath, CompanyPath);
            Assert.AreEqual(HoldownStandardList.CompanyUpdate.Seeded, update);
            Assert.AreEqual(0, company.CompanyVersion);
            Assert.AreEqual(0, user.Count);
        }

        private static string ThisFolder([System.Runtime.CompilerServices.CallerFilePath] string file = "") => Path.GetDirectoryName(file);

        /// <summary>The file shipped in the source loads (version 0, empty until the company list is chosen).</summary>
        [TestMethod]
        public void PackagedDefaultInTheSource_Loads()
        {
            var src = Path.GetFullPath(Path.Combine(ThisFolder(), "..", "ShearWallVisualizer", HoldownStandardList.PackagedFileName));
            Assert.IsTrue(File.Exists(src), src);
            var list = HoldownStandardList.Load(src);
            Assert.IsFalse(list.Customized);
            Assert.IsTrue(list.CompanyVersion >= 0);
            Assert.AreEqual(0, new ShearWallDesigner { StandardList = list }.StandardKeysNotInCatalog().Count, "every shipped key is in the catalog");
        }

        [TestMethod]
        public void ProjectRecord_ListsTheWallsWhoseHoldownChanged_AndSavesWithTheProject()
        {
            var designer = new ShearWallDesigner();
            designer.StandardList.Set(ShearWallDesigner.OptionKey("HDUE5-SDS3", 0), true);
            designer.StandardList.MarkAsCompanyVersion(2, "2026-11-02");
            var settings = new DesignSettings { Method = DesignMethods.ASD, HoldownsFromStandardList = true };
            var wall = new WallData(10, 0, 0, 4, 0);
            var before = new Dictionary<int, WallDesignResult> { [1] = designer.Check(1, wall, 2.2, null, settings), [2] = designer.Check(2, new WallData(10, 0, 0, 20, 0), 0.5, null, settings, new WallDesignInputs { DeadLoad = 1000 }) };
            Assert.AreEqual("HDUE5-SDS3#1", before[1].Holdown.Key);

            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc.DesignSettings = settings;
            calc.DesignSettings.HoldownList = HoldownListRecord.Make(before, designer.StandardList, true);
            var read = TestProject.RoundTrip(calc).DesignSettings.HoldownList;
            Assert.AreEqual(2, read.CompanyVersion);
            Assert.AreEqual("2026-11-02", read.CompanyDate);
            Assert.IsFalse(read.Customized);
            Assert.AreEqual("HDUE5-SDS3#1", read.WallHoldowns[1]);
            Assert.AreEqual("", read.WallHoldowns[2], "no uplift, no hold-down");
            Assert.AreEqual(0, read.Differences(before).Count);

            // company list v3 drops HDUE5 for HDUE9: wall 1 now gets HDUE9
            var v3 = new HoldownStandardList();
            v3.Set(ShearWallDesigner.OptionKey("HDUE9-SDS3.5", 0), true);
            designer.StandardList.RestoreFrom(v3);
            var after = new Dictionary<int, WallDesignResult> { [1] = designer.Check(1, wall, 2.2, null, settings), [2] = before[2] };
            CollectionAssert.AreEqual(new[] { "Wall 1: HDUE5-SDS3#1 → HDUE9-SDS3.5#1" }, read.Differences(after));
            Assert.AreEqual("company list v2 (2026-11-02)", HoldownListRecord.Describe(read.CompanyVersion, read.CompanyDate, read.Customized));
        }

        [TestMethod]
        public void OlderProject_HasNoRecord()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            var read = TestProject.RoundTrip(calc).DesignSettings;
            Assert.IsNull(read.HoldownList);
            Assert.IsFalse(read.HoldownsFromStandardList);
        }
    }
}
