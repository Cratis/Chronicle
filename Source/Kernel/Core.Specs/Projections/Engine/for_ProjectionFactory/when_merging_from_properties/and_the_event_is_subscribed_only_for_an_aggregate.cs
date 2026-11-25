// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_merging_from_properties;

public class and_the_event_is_subscribed_only_for_an_aggregate : given.a_read_model_and_event_schema
{
    List<KeyValuePair<PropertyPath, string>> _result;

    void Because() => _result = ProjectionFactory.GetMergedFromProperties(FromWith(("total", "$increment(count)")), _readModelSchema, _eventSchema, AutoMap.Enabled, _noAutoMap);

    [Fact] void should_not_auto_map_other_properties() => _result.Select(_ => _.Key.Path).ShouldEqual(["total"]);
}
