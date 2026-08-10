// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Chronicle.XUnit.Integration.Packaging.for_the_embedded_kernel_closure.when_reading_project_references;

public class with_self_closing_attribute_metadata : Specification
{
    const string ProjectPath = "../Attribute/Attribute.csproj";
    IReadOnlyCollection<(string Include, bool IsEmbedded)> _references;

    void Because()
    {
        var document = XDocument.Parse($$"""
            <Project>
                <ItemGroup>
                    <ProjectReference Include="{{ProjectPath}}" privateassets=" compile ; ALL ; runtime " />
                </ItemGroup>
            </Project>
            """);

        (_, _references) = ProjectFileDependencies.Read(document);
    }

    [Fact] void should_retain_the_include() => _references.Single().Include.ShouldEqual(ProjectPath);
    [Fact] void should_recognize_the_reference_as_embedded() => _references.Single().IsEmbedded.ShouldBeTrue();
}
