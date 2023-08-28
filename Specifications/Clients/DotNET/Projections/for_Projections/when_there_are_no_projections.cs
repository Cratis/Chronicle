// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections.for_Projections;

public class when_there_are_no_projections : given.all_dependencies
{
    Projections projections;
    IEnumerable<ProjectionDefinition> result;

    void Establish()
    {
        client_artifacts.Setup(_ => _.Projections).Returns(Array.Empty<Type>());
        projections = new Projections(
            event_types.Object,
            client_artifacts.Object,
            schema_generator.Object,
            model_name_resolver.Object,
            service_provider.Object,
            json_serializer_options);
    }

    void Because() => result = projections.Definitions;

    [Fact] void should_return_empty_list() => result.ShouldBeEmpty();
}
