// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

/// <summary>
/// The factory receives only the latest schema of each event type; an earlier generation, possibly pinned, may still
/// declare the property, so the key cannot be rewritten without it.
/// </summary>
public class and_only_the_latest_generation_is_known : given.a_projection_definition
{
    EventType _pinned;

    void Establish()
    {
        _pinned = new(_type.Id, 1);
        _schemas = [Schema(new(_type.Id, 2), "name")];
        _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_pinned] = From() });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_keep_the_key() => _result.From[_pinned].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_not_report_a_rewrite() => _rewritten.ShouldBeEmpty();
}
