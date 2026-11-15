// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class without_named_tags : given.a_builder
{
    void Because() => _result = _builder.Build();
    [Fact] void should_not_filter_on_tags() => _result.NamedTags.ShouldBeEmpty();
}
