// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

/// <summary>
/// A clear has to reach the changeset as a change, not as a skipped write. The difference decides whether the
/// stored document is updated at all - a clear that produces no difference leaves the stale value in the sink.
/// </summary>
public class when_mapping_no_value_over_an_existing_value : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    AppendedEvent _event;
    ExpandoObject _target;
    PropertyDifference _result;

    void Establish()
    {
        _target = new();
        _event = new(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());

        dynamic target = _target;
        target.command = "Ready";
        _propertyMapper = PropertyMappers.FromEventValueProvider("command", EventValueProviders.Null);
    }

    void Because() => _result = _propertyMapper(_event, _target, ArrayIndexers.NoIndexers);

    [Fact] void should_clear_the_property() => ((IDictionary<string, object?>)_target)["command"].ShouldBeNull();
    [Fact] void should_report_the_original_value() => _result.Original.ShouldEqual("Ready");
    [Fact] void should_report_no_changed_value() => _result.Changed.ShouldBeNull();
    [Fact] void should_be_a_change() => _result.HasChanges().ShouldBeTrue();
}
