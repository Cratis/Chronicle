// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_a_near_miss_follows_a_self_reducer : given.near_misses
{
    void Establish()
    {
        _artifacts.Reducers.Returns([typeof(PlainSelf), _nearMiss]);
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.RegisterAll();
    }

    [Fact] void should_keep_only_the_accepted_reducer() => _store.Reducers.GetAllHandlers().Select(handler => handler.ReducerType).ShouldContainOnly(typeof(PlainSelf));
    [Fact] void should_warn_about_the_skipped_candidate() => WarnedAboutNearMiss().ShouldBeTrue();
}
