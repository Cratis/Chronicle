// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections.for_ClientProjections.given;

public class client_projections_without_any_definitions : all_dependencies
{
    protected ClientProjections definitions;

    void Establish()
    {
        projections = new();
        immediate_projections = new();
        adapters = new();
        rules_projections = new();

        projections.Setup(_ => _.Definitions).Returns(ImmutableList<ProjectionDefinition>.Empty);
        immediate_projections.Setup(_ => _.Definitions).Returns(ImmutableList<ProjectionDefinition>.Empty);
        adapters.Setup(_ => _.Definitions).Returns(ImmutableList<ProjectionDefinition>.Empty);
        rules_projections.Setup(_ => _.Definitions).Returns(ImmutableList<ProjectionDefinition>.Empty);

        definitions = new(
            projections.Object,
            immediate_projections.Object,
            adapters.Object,
            rules_projections.Object);
    }
}
