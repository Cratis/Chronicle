// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_named_tags : given.a_builder
{
    void Establish() => _builder.WithNamedTag("audience", "lobby-a").WithNamedTag("audience", "lobby-b").WithNamedTag("audience", "lobby-a").WithNamedTag("region", "eu");
    void Because() => _result = _builder.Build();
    [Fact] void should_keep_every_distinct_tag() => _result.NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldContainOnly(("audience", "lobby-a"), ("audience", "lobby-b"), ("region", "eu"));
}
