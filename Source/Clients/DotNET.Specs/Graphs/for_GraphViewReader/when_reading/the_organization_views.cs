// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class the_organization_views : given.a_graph_view_reader
{
    string _fixture;
    string _fixtureWithoutSchema;
    GraphViews _views;
    string _withSchema;
    string _default;

    void Establish()
    {
        _fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Graphs", "for_GraphViewReader", "Fixtures", "graph-views-organization.json"));
        var lines = _fixture.Split('\n').ToList();
        var start = lines.IndexOf("  \"schema\": {");
        var end = lines.IndexOf("  },", start);
        lines.RemoveRange(start, end - start + 1);
        _fixtureWithoutSchema = string.Join('\n', lines);
    }

    void Because()
    {
        _views = Read(OrganizationSchema, OrganizationViews);
        _withSchema = GraphViewDefinitionSerializer.Serialize(_views.Definitions[0], includeSchema: true);
        _default = GraphViewDefinitionSerializer.Serialize(_views.Definitions[0]);
    }

    [Fact] void should_accept_every_view() => _views.Failures.ShouldBeEmpty();
    [Fact] void should_have_one_definition_for_the_graph() => _views.Definitions.Single().Graph.ShouldEqual("Organization");
    [Fact] void should_write_the_schema_included_form_exactly_as_the_fixture() => _withSchema.ShouldEqual(_fixture);
    [Fact] void should_write_the_default_form_as_the_fixture_without_its_schema() => _default.ShouldEqual(_fixtureWithoutSchema);
}
