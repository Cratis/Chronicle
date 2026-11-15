// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_a_graph;

public class and_generating_it_back : given.an_organization_graph_declaration
{
    CompiledGraph _original;
    CompiledGraph _recompiled;
    string _generated;
    string _generatedAgain;
    CompilerErrors _errors;

    void Because()
    {
        _original = CompileGraph(Declaration, out _errors);
        _generated = _languageService.Generate(_original.Projection, _original.ReadModel);
        _recompiled = CompileGraph(_generated, out _errors);
        _generatedAgain = _languageService.Generate(_recompiled.Projection, _recompiled.ReadModel);
    }

    [Fact] void should_compile_what_was_generated() => _errors.ShouldBeNull();
    [Fact] void should_generate_the_same_text_from_the_recompiled_definition() => _generatedAgain.ShouldEqual(_generated);
    [Fact] void should_keep_the_nodes() => Describe(_recompiled.Projection.Graph!.Nodes).ShouldEqual(Describe(_original.Projection.Graph!.Nodes));
    [Fact] void should_keep_the_edges() => Describe(_recompiled.Projection.Graph!.Edges).ShouldEqual(Describe(_original.Projection.Graph!.Edges));
    [Fact] void should_keep_the_graph_schema() => PrimitiveSchema(_recompiled.ReadModel).ShouldEqual(PrimitiveSchema(_original.ReadModel));
    [Fact] void should_declare_the_graph() => _generated.ShouldContain("graph Organization");
    [Fact] void should_declare_the_projection() => _generated.ShouldContain("projection OrganizationGraph => graph Organization");
    [Fact] void should_write_the_removal_with_detach() => _generated.ShouldContain("remove with PersonRemoved key $eventSourceId detach");
    [Fact] void should_write_the_counter() => _generated.ShouldContain("increment memberCount");
    [Fact] void should_write_the_event_context_mapping() => _generated.ShouldContain("since = $eventContext.occurred");
    [Fact] void should_write_the_identity() => _generated.ShouldContain("identified by visitId");

    static string Describe<T>(IDictionary<string, T> elements) =>
        string.Join(';', elements.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}={System.Text.Json.JsonSerializer.Serialize(_.Value, _.Value!.GetType())}"));

    static string PrimitiveSchema(ReadModelDefinition readModel) => readModel.GetSchemaForLatestGeneration().ToJson();
}
