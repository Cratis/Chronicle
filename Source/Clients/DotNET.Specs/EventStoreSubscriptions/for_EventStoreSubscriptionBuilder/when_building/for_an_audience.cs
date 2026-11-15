// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class for_an_audience : given.a_builder
{
    void Establish() => ((Cratis.Chronicle.EventStoreSubscriptions.IEventStoreSubscriptionBuilder)_builder).ForAudience("lobby-a");
    void Because() => _result = _builder.Build();
    [Fact] void should_filter_on_the_audience_tag() => _result.NamedTags.Single().ShouldEqual(new NamedTag(new TagName(WellKnownTags.Audience), "lobby-a"));
}
