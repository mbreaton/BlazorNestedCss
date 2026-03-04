namespace BlazorNestedCss.Tasks;

using System.Diagnostics;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class RewriteCss : Task
{
    [Required]
    public ITaskItem[] FilesToTransform { get; set; } = [];

    public override bool Execute()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        var info = new FileInfo(assembly.Location);
        var lastBuiltOnDate = info.LastWriteTime.ToString("yyyy-MM-dd @HH:mm");

        Log.LogMessage(MessageImportance.High, $"************************************");
        Log.LogMessage(MessageImportance.High, $"*** Starting Blazor CSS Rewriter ***");
        Log.LogMessage(MessageImportance.High, $"*** Version: {version,-13}       ***");
        Log.LogMessage(MessageImportance.High, $"*** Built on: {lastBuiltOnDate}  ***");

        var parser = new CssParser();
        var rewriter = new BlazorScopeRewriter();

        var filesProceesed = 0;
        Parallel.ForEach(FilesToTransform, file =>
        {
            try
            {
                var inputFile = file.GetMetadata("FullPath");
                var outputFile = file.GetMetadata("OutputFile");
                var cssScope = file.GetMetadata("CssScope");

                // only build if inputFile last modified time is newer than outputFile time
                if (File.Exists(outputFile))
                {
                    var inputLastWrite = File.GetLastWriteTimeUtc(inputFile);
                    var outputLastWrite = File.GetLastWriteTimeUtc(outputFile);
                    if (outputLastWrite > inputLastWrite)
                    {
                        return;
                    }
                }

                var path = inputFile;
                var text = File.ReadAllText(path);

                var css = parser.Parse(text);

                rewriter.Rewrite(css, cssScope);

                var rewritten = parser.Generate(css);

                // ensure the destination folder exist, create if not
                var fi = new FileInfo(outputFile);
                if (!Directory.Exists(fi.DirectoryName))
                {
                    Directory.CreateDirectory(fi.DirectoryName!);
                }

                File.WriteAllText(outputFile, rewritten);

                filesProceesed++;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to rewrite CSS file '{file.ItemSpec}': {ex.Message}");
                throw;
            }
        });

        var fp = filesProceesed;
        Log.LogMessage(MessageImportance.High, $"*** Added scope to {fp,5} files.  ***");
        Log.LogMessage(MessageImportance.High, $"*** Revisit after SDK 10.0.103   ***");
        Log.LogMessage(MessageImportance.High, $"************************************");

        return true;
    }
}