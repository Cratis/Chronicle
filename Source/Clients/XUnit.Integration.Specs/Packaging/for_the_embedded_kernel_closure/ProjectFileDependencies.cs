// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Chronicle.XUnit.Integration.Packaging.for_the_embedded_kernel_closure;

static class ProjectFileDependencies
{
    internal static (HashSet<string> Packages, IReadOnlyCollection<(string Include, bool IsEmbedded)> References) Read(FileInfo project) =>
        Read(XDocument.Load(project.FullName));

    internal static (HashSet<string> Packages, IReadOnlyCollection<(string Include, bool IsEmbedded)> References) Read(XDocument document)
    {
        // A package reference marked PrivateAssets=all contributes nothing to whoever consumes this project, and
        // that is how a build-only package - a generator, an analyzer - says it has no runtime assets to carry.
        // Requiring the packaging project to declare one would make every consumer run our proxy generator.
        var packages = document.Descendants()
            .Where(_ => HasName(_.Name, "PackageReference") && !MetadataValues(_, "PrivateAssets").Any(IncludesAll))
            .Select(_ => AttributeValue(_, "Include"))
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var references = document.Descendants()
            .Where(_ => HasName(_.Name, "ProjectReference"))
            .Select(reference => (
                Include: AttributeValue(reference, "Include"),
                IsEmbedded: MetadataValues(reference, "PrivateAssets").Any(IncludesAll)))
            .Where(_ => _.Include is not null)
            .Select(_ => (Include: _.Include!, _.IsEmbedded))
            .ToArray();

        return (packages, references);
    }

    static string? AttributeValue(XElement element, string name) =>
        element.Attributes().FirstOrDefault(_ => HasName(_.Name, name))?.Value;

    static IEnumerable<string> MetadataValues(XElement element, string name) =>
        element.Attributes()
            .Where(_ => HasName(_.Name, name))
            .Select(_ => _.Value)
            .Concat(element.Elements().Where(_ => HasName(_.Name, name)).Select(_ => _.Value));

    static bool IncludesAll(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains("all", StringComparer.OrdinalIgnoreCase);

    static bool HasName(XName actual, string expected) =>
        actual.LocalName.Equals(expected, StringComparison.OrdinalIgnoreCase);
}
