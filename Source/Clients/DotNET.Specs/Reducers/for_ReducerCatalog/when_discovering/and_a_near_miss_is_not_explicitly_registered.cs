// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_a_near_miss_is_not_explicitly_registered : given.near_misses
{
    Exception _explicitError;

    void Establish()
    {
        _artifacts.Reducers.Returns([_nearMiss]);
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.RegisterAll();
        _explicitError = await Catch.Exception(() => _store.Reducers.Register(_nearMiss));
    }

    [Fact] void should_leave_discovery_nonfatal_and_unregistered() => _store.Reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_refuse_the_same_signature_on_explicit_registration() => _explicitError.ShouldBeOfExactType<InvalidSelfReducerSignature>();
    [Fact] void should_warn_about_the_skipped_candidate() => WarnedAboutNearMiss().ShouldBeTrue();
    [Fact] void should_not_recognize_a_supported_shape() => SelfReducerMethods.HasSelfShape(_nearMiss).ShouldBeFalse();
}
