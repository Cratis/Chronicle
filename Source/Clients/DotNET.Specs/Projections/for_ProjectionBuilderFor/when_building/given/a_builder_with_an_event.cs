// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given;

public class a_builder_with_an_event : a_projection_builder
{
    protected override IEnumerable<Type> EventTypes => [typeof(ProjectionEvent)];
}
