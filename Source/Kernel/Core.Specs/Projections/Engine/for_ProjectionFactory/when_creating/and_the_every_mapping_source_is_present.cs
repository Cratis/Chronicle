// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_the_every_mapping_source_is_present : given.a_projection_with_payload_every_mappings
{
    void Establish() => _definition.FromEvery.Properties["lastValue"] = "details.value";

    Task Because() => Project(_valueChanged, new { details = new { value = "updated" } }.AsExpandoObject());

    [Fact] void should_map_the_source_value() => _result["lastValue"].ShouldEqual("updated");
}
