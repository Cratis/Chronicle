// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_merging_from_properties;

public class and_a_property_is_marked_no_auto_map : given.a_read_model_and_event_schema
{
    List<KeyValuePair<PropertyPath, string>> _result;

    void Establish() => _noAutoMap = new HashSet<string> { "name" };

    void Because() => _result = ProjectionFactory.GetMergedFromProperties(FromWith(("other", "unrelated")), _readModelSchema, _eventSchema, AutoMap.Enabled, _noAutoMap);

    [Fact] void should_not_auto_map_the_excluded_property() => _result.Exists(_ => _.Key.Path == "name").ShouldBeFalse();
    [Fact] void should_auto_map_the_other_property() => _result.ShouldContain(_ => _.Key.Path == "total");
}
