// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_an_unregistered_event_lacks_the_every_mapping_source : given.a_projection_with_payload_every_mappings
{
    void Establish()
    {
        _definition.From.Clear();
        _definition.Children.Clear();
        _definition = _definition with { SubscribesToAllEvents = true };
    }

    Task Because() => Project(_unrelated, new ExpandoObject());

    [Fact] void should_accept_the_unregistered_event() => _projection.Accepts(_unrelated).ShouldBeTrue();
    [Fact] void should_keep_the_existing_value() => _result["lastValue"].ShouldEqual("kept");
    [Fact] void should_still_count_the_event() => _result["count"].ShouldEqual(1L);
}
