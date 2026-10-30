// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_source_keys_are_mixed_with_shared_keys : given.a_projection_with_source_keys
{
    void Establish() => _definition.From[_second] = new(new Dictionary<PropertyPath, string>(), "$value(shared)", null);

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_require_every_resolver_to_be_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
