// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_merging_from_properties;

public class and_a_property_has_an_explicit_mapping : given.a_read_model_and_event_schema
{
    List<KeyValuePair<PropertyPath, string>> _result;

    void Because() => _result = ProjectionFactory.GetMergedFromProperties(FromWith(("name", "unrelated")), _readModelSchema, _eventSchema, AutoMap.Enabled, _noAutoMap);

    [Fact] void should_keep_the_explicit_mapping() => _result.Single(_ => _.Key.Path == "name").Value.ShouldEqual("unrelated");
    [Fact] void should_auto_map_the_remaining_name_matched_property() => _result.ShouldContain(_ => _.Key.Path == "total" && _.Value == "total");
}
