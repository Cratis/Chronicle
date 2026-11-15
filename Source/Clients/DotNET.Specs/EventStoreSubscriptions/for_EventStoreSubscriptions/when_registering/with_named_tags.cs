// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions.when_registering;

public class with_named_tags : given.subscriptions
{
    async Task Because() => await _subscriptions.Subscribe("s", "source", builder => builder.WithEventType<CustomerRegistered>().ForAudience("lobby-a"));
    [Fact] void should_send_the_tag_filter() => _request.NamedTags.Select(_ => (_.Name, _.Value)).ShouldContainOnly((WellKnownTags.Audience, "lobby-a"));
}
