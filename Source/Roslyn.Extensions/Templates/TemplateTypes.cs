using HandlebarsDotNet;

namespace Roslyn.Extensions.Templates;

public static class TemplateTypes
{
    public static readonly HandlebarsTemplate<object, object> Metrics = Handlebars.Compile(GetTemplate("Metrics"));

    static string GetTemplate(string name)
    {
        var rootType = typeof(TemplateTypes);
        var stream = rootType.Assembly.GetManifestResourceStream($"{rootType.Namespace}.{name}.hbs");
        if (stream != default)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return string.Empty;
    }
}