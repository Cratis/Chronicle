// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_merging_from_properties;

public class and_the_event_schema_is_unknown : given.a_read_model_and_event_schema
{
    List<KeyValuePair<PropertyPath, string>> _result;

    void Because() => _result = ProjectionFactory.GetMergedFromProperties(FromWith(("other", "unrelated")), _readModelSchema, null, AutoMap.Enabled, _noAutoMap);

    [Fact] void should_keep_only_the_explicit_mappings() => _result.Select(_ => _.Key.Path).ShouldEqual(["other"]);
}
