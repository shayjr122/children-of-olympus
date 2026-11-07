using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GMR.Tests
{
    /// <summary>
    /// Text scanner behind BannedApiTests (AD-3, AD-5). Comments and string literals are blanked before matching.
    /// </summary>
    public static class BannedApiScanner
    {
        public readonly struct Rule
        {
            public readonly string Name;
            public readonly Regex Pattern;

            public Rule(string name, string pattern)
            {
                Name = name;
                Pattern = new Regex(pattern, RegexOptions.Compiled);
            }
        }

        public static readonly IReadOnlyList<Rule> Rules = new[]
        {
            new Rule("UnityEngine.Random", @"\bUnityEngine\s*\.\s*Random\b"),
            new Rule("Random.Range", @"(?<![\w\.])Random\s*\.\s*Range\b"),
            new Rule("Random.value", @"(?<![\w\.])Random\s*\.\s*value\b"),
            new Rule("Random.insideUnitCircle", @"(?<![\w\.])Random\s*\.\s*insideUnitCircle\b"),
            new Rule("new System.Random", @"\bnew\s+(?:System\s*\.\s*)?Random\s*\("),
            new Rule("Guid.NewGuid", @"\bGuid\s*\.\s*NewGuid\b"),
            new Rule("DateTime.Now", @"\bDateTime\s*\.\s*Now\b"),
            new Rule("static event", @"\bstatic\s+event\b"),
            // A static field that is neither const nor readonly: "static <type> <name> = ..." or "static <type> <name>;".
            new Rule("mutable static field",
                @"\bstatic\s+(?!readonly\b|class\b|partial\b|extern\b|event\b|void\b)(?:volatile\s+)?[\w\.]+(?:<[\w\s,\.<>\[\]\?]*>)?(?:\[[\s,]*\])*\??\s+\w+\s*(?:=(?![=>])|;)"),
        };

        public readonly struct Violation
        {
            public readonly string File;
            public readonly int Line;
            public readonly string Rule;
            public readonly string Text;

            public Violation(string file, int line, string rule, string text)
            {
                File = file;
                Line = line;
                Rule = rule;
                Text = text;
            }

            public override string ToString() => $"{File}:{Line}: banned {Rule}: {Text}";
        }

        /// <summary>True if <paramref name="relativePath"/> (relative to Assets/Scripts, '/'-separated) is allow-listed.</summary>
        public static bool IsAllowListed(string relativePath)
        {
            string p = relativePath.Replace('\\', '/');
            return p.StartsWith("DevTools/") || p == "RngStreams.cs" || p.EndsWith("/RngStreams.cs");
        }

        public static List<Violation> Scan(string displayPath, string source)
        {
            var violations = new List<Violation>();
            string[] code = StripCommentsAndStrings(source).Split('\n');
            string[] original = source.Split('\n');
            for (int i = 0; i < code.Length; i++)
            {
                foreach (var rule in Rules)
                {
                    if (rule.Pattern.IsMatch(code[i]))
                        violations.Add(new Violation(displayPath, i + 1, rule.Name, original[i].Trim()));
                }
            }
            return violations;
        }

        /// <summary>Blanks comments and string/char literal contents with spaces, keeping line breaks.</summary>
        public static string StripCommentsAndStrings(string s)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                char n = i + 1 < s.Length ? s[i + 1] : '\0';
                if (c == '/' && n == '/')
                {
                    while (i < s.Length && s[i] != '\n') { sb.Append(' '); i++; }
                }
                else if (c == '/' && n == '*')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < s.Length && !(s[i] == '*' && i + 1 < s.Length && s[i + 1] == '/'))
                    {
                        sb.Append(s[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                    if (i < s.Length) { sb.Append("  "); i += 2; }
                }
                else if (c == '"' || c == '\'')
                {
                    bool verbatim = c == '"' && i > 0 && s[i - 1] == '@';
                    char quote = c;
                    sb.Append(quote);
                    i++;
                    while (i < s.Length)
                    {
                        char d = s[i];
                        if (verbatim && d == '"' && i + 1 < s.Length && s[i + 1] == '"') { sb.Append("  "); i += 2; continue; }
                        if (!verbatim && d == '\\' && i + 1 < s.Length) { sb.Append("  "); i += 2; continue; }
                        if (d == quote) { sb.Append(quote); i++; break; }
                        if (d == '\n' && !verbatim) break;
                        sb.Append(d == '\n' ? '\n' : ' ');
                        i++;
                    }
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}
