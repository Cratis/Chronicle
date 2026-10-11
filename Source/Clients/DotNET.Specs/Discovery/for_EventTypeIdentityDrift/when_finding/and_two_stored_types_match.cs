// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Discovery.for_EventTypeIdentityDrift.when_finding;

public class and_two_stored_types_match : given.a_renamed_event_type
{
    void Establish()
    {
        _stored = [.. _stored, new() { Type = new() { Id = "Another" }, Owner = EventTypeOwner.Client, Schema = _stored[0].Schema }];
    }

    void Because() => _result = EventTypeIdentityDrift.Find(_stored, _current, _ => _schema).ToArray();

    [Fact] void should_report_only_unambiguous_shape_matches() => _result.ShouldBeEmpty();
}
