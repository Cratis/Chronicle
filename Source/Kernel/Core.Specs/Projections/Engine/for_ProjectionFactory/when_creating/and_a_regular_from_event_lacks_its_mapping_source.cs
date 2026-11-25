// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_regular_from_event_lacks_its_mapping_source : given.a_projection_with_payload_every_mappings
{
    void Establish()
    {
        _definition = _definition
            .WithoutEveryProperty("lastValue")
            .WithFromProperty(_valueChanged, "lastValue", "value");
    }

    Task Because() => Project(_valueChanged, new ExpandoObject());

    [Fact] void should_still_clear_the_value() => _result["lastValue"].ShouldBeNull();
}
