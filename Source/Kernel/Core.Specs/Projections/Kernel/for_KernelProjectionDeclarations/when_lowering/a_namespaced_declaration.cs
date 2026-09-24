// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.when_lowering;

public class a_namespaced_declaration : Specification
{
    ReadModelDefinition _readModel;
    IReadOnlyCollection<ProjectionDefinition> _projections;

    void Because() => (_readModel, _projections) = KernelProjectionDeclarations.Lower(typeof(given.a_namespaced_declaration));

    [Fact] void should_lower_to_one_definition() => _projections.Count.ShouldEqual(1);
    [Fact] void should_prefix_the_identifier() => _projections.First().Identifier.Value.ShouldEqual("$system.a-namespaced-one");
    [Fact] void should_be_owned_by_the_kernel() => _projections.First().Owner.ShouldEqual(ProjectionOwner.Kernel);
    [Fact] void should_not_be_rewindable() => _projections.First().IsRewindable.ShouldBeFalse();
    [Fact] void should_be_namespaced() => _projections.First().Scope.ShouldEqual(ProjectionScope.Namespaced);
    [Fact] void should_name_the_read_model_after_its_type() => _projections.First().ReadModel.Value.ShouldEqual(nameof(given.a_namespaced_read_model));

    /// <summary>
    /// Schema property names are camel cased while projection property paths are Pascal cased - the same split the
    /// client already has, because the schema describes the serialized document and the path names the CLR member.
    /// </summary>
    [Fact] void should_describe_the_read_model_with_a_generated_schema() => _readModel.GetSchemaForLatestGeneration().Properties.Keys.ShouldContainOnly(["value", "count"]);
    [Fact] void should_own_the_read_model_as_the_server() => _readModel.Owner.ShouldEqual(ReadModelOwner.Server);
    [Fact] void should_source_the_read_model_from_code() => _readModel.Source.ShouldEqual(ReadModelSource.Code);
    [Fact] void should_observe_the_read_model_with_a_projection() => _readModel.ObserverType.ShouldEqual(ReadModelObserverType.Projection);
}
