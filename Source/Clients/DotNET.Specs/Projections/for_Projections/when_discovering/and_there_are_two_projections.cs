// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_there_are_two_projections : given.all_dependencies
{
    public record FirstModel();
    public record SecondModel();

    Mock<IProjectionFor<FirstModel>> first_projection;
    Mock<IProjectionFor<SecondModel>> second_projection;

    Projections projections;
    IEnumerable<ProjectionDefinition> result;

    void Establish()
    {
        first_projection = new();
        second_projection = new();

        client_artifacts.Setup(_ => _.Projections).Returns(
        [
            first_projection.Object.GetType(),
            second_projection.Object.GetType()
        ]);

        service_provider.Setup(_ => _.GetService(first_projection.Object.GetType())).Returns(first_projection.Object);
        service_provider.Setup(_ => _.GetService(second_projection.Object.GetType())).Returns(second_projection.Object);

        schema_generator.Setup(_ => _.Generate(IsAny<Type>())).Returns(new JsonSchema());

        projections = new Projections(
            event_store.Object,
            event_types.Object,
            client_artifacts.Object,
            rules_projections.Object,
            schema_generator.Object,
            model_name_resolver.Object,
            event_serializer.Object,
            service_provider.Object,
            json_serializer_options);

        rules_projections.Setup(_ => _.Discover()).Returns(ImmutableList<ProjectionDefinition>.Empty);
    }

    async Task Because()
    {
        await projections.Discover();
        result = projections.Definitions;
    }

    [Fact] void should_return_two_definitions() => result.Count().ShouldEqual(2);
    [Fact] void should_call_define_on_first_model_builder() => first_projection.Verify(_ => _.Define(IsAny<IProjectionBuilderFor<FirstModel>>()), Once);
    [Fact] void should_call_define_on_second_model_builder() => second_projection.Verify(_ => _.Define(IsAny<IProjectionBuilderFor<SecondModel>>()), Once);
}
