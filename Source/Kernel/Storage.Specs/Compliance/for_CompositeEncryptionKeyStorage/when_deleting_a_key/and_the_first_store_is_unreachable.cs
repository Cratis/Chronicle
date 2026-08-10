// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_deleting_a_key;

public class and_the_first_store_is_unreachable : given.two_key_stores
{
    Exception _error;
    bool _secondaryHasIt;

    async Task Establish()
    {
        await Save(_secondary, KeyNamed("secondary"));
        _composite = new CompositeEncryptionKeyStorage(AnUnreachableStore(), _secondary);
    }

    async Task Because()
    {
        // Stopping at the first failure would leave the key alive in a store that was never even attempted, while
        // the caller sees the same exception either way.
        _error = await Catch.Exception(async () => await _composite.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _secondaryHasIt = await HasKeyIn(_secondary);
    }

    [Fact] void should_still_erase_it_from_every_reachable_store() => _secondaryHasIt.ShouldBeFalse();
    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_carry_the_failure_from_the_unreachable_store() => ((EncryptionKeyErasureIncomplete)_error).Failures.ShouldContainOnly(_error.InnerException);
}
