// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierSpanProcessor;

public class when_omitting_identifiers : Specification
{
    Activity _activity;

    void Establish()
    {
        _activity = new Activity("spec")
            .SetTag("orleans.rpc.target_id", "observer-subscriber/abc")
            .SetTag("orleans.rpc.source_id", "observer-subscriber/def")
            .SetTag("cratis.eventsource.id", "customer-42");
    }

    void Because() => new IdentifierSpanProcessor(Options.Create(new ChronicleOptions())).OnEnd(_activity);

    [Fact] void should_remove_the_target_id() => _activity.GetTagItem("orleans.rpc.target_id").ShouldBeNull();
    [Fact] void should_remove_the_source_id() => _activity.GetTagItem("orleans.rpc.source_id").ShouldBeNull();
    [Fact] void should_remove_the_event_source_id() => _activity.GetTagItem("cratis.eventsource.id").ShouldBeNull();

    void Destroy() => _activity.Dispose();
}
