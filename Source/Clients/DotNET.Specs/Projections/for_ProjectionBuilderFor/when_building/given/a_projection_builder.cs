// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given;

public class a_projection_builder : Specification
{
    protected ProjectionBuilderFor<ProjectionReadModel> builder;
    protected IEventTypes event_types;

    protected virtual IEnumerable<Type> EventTypes => [];

    void Establish()
    {
        event_types = new EventTypesForSpecifications(EventTypes);
        builder = new ProjectionBuilderFor<ProjectionReadModel>(
            new ProjectionId(typeof(ProjectionReadModel).FullName!),
            typeof(ProjectionReadModel),
            new DefaultNamingPolicy(),
            event_types,
            new JsonSerializerOptions());
    }
}
