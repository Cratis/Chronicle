// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_the_every_mapping_source_has_a_missing_ancestor : given.a_projection_with_payload_every_mappings
{
    void Establish() => _definition.FromEvery.Properties["lastValue"] = "details.value";

    Task Because() => Project(_unrelated, new ExpandoObject());

    [Fact] void should_keep_the_existing_value() => _result["lastValue"].ShouldEqual("kept");
    [Fact] void should_not_create_the_source_ancestor() => ((IDictionary<string, object?>)_incoming.Content).ContainsKey("details").ShouldBeFalse();
}
