// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HandlebarsDotNet;

namespace Roslyn.Extensions.Templates;

/// <summary>
/// Represents the template types.
/// </summary>
public static class TemplateTypes
{
    /// <summary>
    /// Gets the metrics template.
    /// </summary>
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
