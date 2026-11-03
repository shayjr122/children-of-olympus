using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    /// <summary>
    /// AD-3 / AD-5: no unseeded randomness, wall-clock time, GUIDs or mutable statics in gameplay code.
    /// Scans Assets/Scripts/**/*.cs. Allow-list: Scripts/DevTools/** and RngStreams.cs.
    /// </summary>
    public sealed class BannedApiTests
    {
        static string ScriptsRoot => Path.Combine(Application.dataPath, "Scripts");

        [Test]
        public void ProjectScripts_ContainNoBannedApis()
        {
            Assert.That(Directory.Exists(ScriptsRoot), $"Scripts folder not found at {ScriptsRoot}");

            var files = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories);
            Assert.That(files, Is.Not.Empty, "No scripts scanned; the scan root is wrong.");

            var violations = files
                .Select(f => (abs: f, rel: f.Substring(ScriptsRoot.Length + 1).Replace('\\', '/')))
                .Where(f => !BannedApiScanner.IsAllowListed(f.rel))
                .SelectMany(f => BannedApiScanner.Scan("Assets/Scripts/" + f.rel, File.ReadAllText(f.abs)))
                .ToList();

            Assert.That(violations, Is.Empty, "Banned API usage:\n" + string.Join("\n", violations));
        }

        [TestCase("float x = UnityEngine.Random.Range(0f, 1f);", "UnityEngine.Random")]
        [TestCase("int i = Random.Range(0, 3);", "Random.Range")]
        [TestCase("float v = Random.value;", "Random.value")]
        [TestCase("var p = Random.insideUnitCircle;", "Random.insideUnitCircle")]
        [TestCase("var r = new System.Random(4);", "new System.Random")]
        [TestCase("var r = new Random();", "new System.Random")]
        [TestCase("var g = System.Guid.NewGuid();", "Guid.NewGuid")]
        [TestCase("var t = DateTime.Now;", "DateTime.Now")]
        [TestCase("static int s_count;", "mutable static field")]
        [TestCase("private static float s_speed = 3f;", "mutable static field")]
        [TestCase("public static List<int> Cache = new List<int>();", "mutable static field")]
        [TestCase("public static event Action Fired;", "static event")]
        public void Scanner_FlagsBannedPattern_WithFileAndLine(string line, string rule)
        {
            string source = "namespace X {\nclass Y {\n" + line + "\n}\n}";
            var violations = BannedApiScanner.Scan("Assets/Scripts/Runtime/Bad.cs", source);

            Assert.That(violations.Any(v => v.Rule == rule && v.Line == 3),
                $"Expected '{rule}' on line 3, got: {string.Join("; ", violations)}");
            StringAssert.Contains("Assets/Scripts/Runtime/Bad.cs:3", violations.First(v => v.Rule == rule).ToString());
        }

        [TestCase("const int Max = 3;")]
        [TestCase("static readonly int[] Table = { 1 };")]
        [TestCase("public const string Waves = \"waves\";")]
        [TestCase("public static class Util { }")]
        [TestCase("public static int Add(int a, int b) => a + b;")]
        [TestCase("static void Tick() { }")]
        [TestCase("public static T Get<T>() where T : class => null;")]
        [TestCase("public static bool Ready => true;")]
        [TestCase("// Random.Range is banned; DateTime.Now too")]
        [TestCase("string s = \"Random.Range(0, 1)\";")]
        [TestCase("float r = rng.Range(0f, 1f);")]
        [TestCase("var t = DateTime.UtcNow.Ticks;")]
        public void Scanner_IgnoresAllowedCode(string line)
        {
            string source = "namespace X {\nclass Y {\n" + line + "\n}\n}";
            var violations = BannedApiScanner.Scan("Assets/Scripts/Core/Ok.cs", source);
            Assert.That(violations, Is.Empty, string.Join("; ", violations));
        }

        [TestCase("DevTools/DebugMenu.cs", true)]
        [TestCase("DevTools/Sub/Seed.cs", true)]
        [TestCase("Core/Rng/RngStreams.cs", true)]
        [TestCase("Core/Rng/RunRng.cs", false)]
        [TestCase("Runtime/DevToolsLike.cs", false)]
        public void AllowList_MatchesSpec(string relativePath, bool allowed)
        {
            Assert.AreEqual(allowed, BannedApiScanner.IsAllowListed(relativePath));
        }
    }
}
