namespace BlazorNestedCss.Tests;

using BlazorNestedCss.Tasks;

using ExCSS;

#if DEBUG
public class CssRewriteTestFiles
{
    CssParser _parser;
    StylesheetParser _stylesheetChecker;
    string _projectFolder;
    string _testFilesFolder;
    string _testOutputFolder;

    public CssRewriteTestFiles()
    {
        _parser = new();
        _stylesheetChecker = new();

        _projectFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../.."));
        _testFilesFolder = Path.GetFullPath(Path.Combine(_projectFolder, "TestFiles"));
        _testOutputFolder = Path.GetFullPath(Path.Combine(_projectFolder, "TestFiles/out"));
    }

    bool IsValidCss(string css)
    {
        var sheet = _stylesheetChecker.Parse(css);

        return string.IsNullOrWhiteSpace(css) || sheet.StyleRules.Any();
    }

    [Fact]
    public void CssRewrite_Rewrite_TestFiles()
    {
        foreach (var filePath in Directory.GetFiles(_testFilesFolder, "*.css"))
        {
            var cssText = File.ReadAllText(filePath);
            Assert.True(IsValidCss(cssText));

            var root = _parser.Parse(cssText);
            new BlazorScopeRewriter().Rewrite(root, "b-scope");
            var output = _parser.Generate(root);
            Assert.True(IsValidCss(output));
            var outPath = filePath.Replace(_testFilesFolder, _testOutputFolder + "/TestFiles");

            var fi = new FileInfo(outPath);
            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName!);
            var outFile = fi.FullName + ".gen.css";
            File.WriteAllText(outFile, output);
        }
    }
}
#endif
