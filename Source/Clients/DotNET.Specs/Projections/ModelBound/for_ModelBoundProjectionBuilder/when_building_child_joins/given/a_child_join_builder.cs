// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins.given;

public class a_child_join_builder : Specification
{
    internal ModelBoundProjectionBuilder _builder;

    void Establish() => _builder = new ModelBoundProjectionBuilder(
        new for_ModelBoundProjectionBuilder.given.CamelCasingNamingPolicy(),
        new EventTypesForSpecifications([typeof(ItemAdded), typeof(ProductRegistered)]));
}
