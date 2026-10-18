// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_the_every_mapping_source_is_present_with_null : given.a_projection_with_payload_every_mappings
{
    Task Because()
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = null;
        return Project(_valueChanged, content);
    }

    [Fact] void should_map_null() => _result["lastValue"].ShouldBeNull();
}
