// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_a_projected_type_reused_without_projection : given.a_reused_type_with_an_unprojected_use
{
    void Establish() => UseAddresses(unprojectedFirst: false);

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_keep_the_shared_types_subscription() => _result.ShouldContain("[FromEvent<OrderPlaced>]");
    [Fact] void should_keep_the_shared_types_mapping_attribute() => _result.ShouldContain("[SetFrom<OrderPlaced>(nameof(OrderPlaced.CustomerName))]");
    [Fact] void should_not_report_a_false_conflict() => _result.ShouldNotContain("conflicting projection scopes");
    [Fact] void should_keep_the_projected_mapping_in_the_builder_definition() => _modelBound.Nested["billingAddress"].From.Single().Value.Properties["displayName"].ShouldEqual("customerName");
    [Fact] void should_not_project_the_unannotated_use() => _modelBound.Nested.Keys.ShouldContainOnly("billingAddress");
}
