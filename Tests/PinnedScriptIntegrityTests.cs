using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Tests;

/// <summary>
/// A script pinned with integrity= runs only if the bytes served are exactly the bytes that
/// were hashed. CI builds on windows-latest, where git checks text files out with CRLF line
/// endings: the Sentry bundle from #185 shipped three bytes longer than the file Sentry
/// publishes, every browser refused it, and production reported no browser errors until
/// .gitattributes exempted the bundle from the conversion. This test reads the same checkout
/// the build publishes, so a pinned file whose bytes the checkout changed fails the build
/// instead of reaching production.
/// </summary>
[TestFixture]
public class PinnedScriptIntegrityTests
{
    private static readonly Regex ScriptTag = new(@"<script\b[^>]*>", RegexOptions.IgnoreCase);
    private static readonly Regex Src = new(@"\bsrc=""([^""]+)""");
    private static readonly Regex Integrity = new(@"\bintegrity=""sha384-([^""]+)""");

    private static string AppDirectory()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
        {
            var app = Path.Combine(dir.FullName, "Mundialito");
            if (File.Exists(Path.Combine(app, "Views", "Home", "Index.cshtml")))
            {
                return app;
            }
        }
        throw new InvalidOperationException("no Mundialito/Views/Home/Index.cshtml above " + TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void EveryPinnedScriptIsTheFileItsHashNames()
    {
        var app = AppDirectory();
        var page = File.ReadAllText(Path.Combine(app, "Views", "Home", "Index.cshtml"));
        var pinned = ScriptTag.Matches(page)
            .Select(tag => (Src: Src.Match(tag.Value), Hash: Integrity.Match(tag.Value)))
            .Where(x => x.Src.Success && x.Hash.Success && !Regex.IsMatch(x.Src.Groups[1].Value, "^(https?:)?//"))
            .Select(x => (Src: x.Src.Groups[1].Value, Hash: x.Hash.Groups[1].Value))
            .ToList();

        // Nothing matched would pass vacuously - say, if the tag's attributes were reformatted.
        Assert.That(pinned, Is.Not.Empty, "Index.cshtml pins no script with integrity=sha384-...");
        Assert.Multiple(() =>
        {
            foreach (var (src, hash) in pinned)
            {
                var file = new FileInfo(Path.Combine(app, "wwwroot", src.Replace('/', Path.DirectorySeparatorChar)));
                Assert.That(file, Does.Exist, $"{src} is pinned but is not in wwwroot");
                if (!file.Exists)
                {
                    continue;
                }
                var actual = Convert.ToBase64String(SHA384.HashData(File.ReadAllBytes(file.FullName)));
                Assert.That(actual, Is.EqualTo(hash),
                    $"wwwroot/{src} ({file.Length} bytes) is not the file its integrity hash names, so browsers "
                    + "will refuse to run it. If git converted its line endings on checkout, its path needs -text "
                    + "in .gitattributes.");
            }
        });
    }
}
