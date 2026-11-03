// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cratis.Chronicle.Aspire.Packaging.for_PackageReadme;

public partial class when_resolving_shared_metadata : Specification
{
    XDocument _metadata;
    string _readme;

    void Establish()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Source/Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        var source = Path.Combine(directory!.FullName, "Source");
        _metadata = XDocument.Load(Path.Combine(source, "Directory.Build.props"));
        _readme = File.ReadAllText(Path.Combine(source, "NUGET.md"));
    }

    [Fact] void should_select_the_readme_in_the_package() => _metadata.Descendants().Single(element => element.Name.LocalName == "PackageReadmeFile").Value.ShouldEqual("README.md");

    [Fact]
    void should_pack_the_registry_readme_under_the_selected_name() => _metadata.Descendants()
        .Single(element => element.Name.LocalName == "None" && (string?)element.Attribute("Include") == "$(MSBuildThisFileDirectory)NUGET.md")
        .Attribute("PackagePath")!.Value.ShouldEqual("/README.md");

    [Fact]
    void should_enable_packing_the_readme() => _metadata.Descendants()
        .Single(element => element.Name.LocalName == "None" && (string?)element.Attribute("Include") == "$(MSBuildThisFileDirectory)NUGET.md")
        .Attribute("Pack")!.Value.ShouldEqual("true");

    [Fact] void should_not_contain_html() => HtmlTags.IsMatch(_readme).ShouldBeFalse();

    [Fact]
    void should_use_absolute_https_links() => Links.Matches(_readme)
        .All(match => Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps).ShouldBeTrue();

    [GeneratedRegex("</?[A-Za-z]", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex HtmlTags { get; }

    [GeneratedRegex(@"\[[^\]]+\]\((?<url>[^)]+)\)", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Links { get; }
}
