// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_a_store_is_unreachable_and_no_other_has_it : given.two_key_stores
{
    EncryptionKey? _result;
    Exception _error;

    void Establish() => _composite = new CompositeEncryptionKeyStorage(AnUnreachableStore(), _secondary);

    async Task Because()
    {
        // An untrue absence is indistinguishable from a completed right-to-erasure: every protected value would
        // read back as an empty string with nothing anywhere saying why.
        _error = await Catch.Exception(async () => _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
    }

    [Fact] void should_fail_rather_than_report_the_key_as_absent() => _error.ShouldBeOfExactType<EncryptionKeyStorageUnavailable>();
    [Fact] void should_not_return_a_key() => _result.ShouldBeNull();
    [Fact] void should_carry_the_failure_from_the_unreachable_store() => ((EncryptionKeyStorageUnavailable)_error).Failures.ShouldContainOnly(_error.InnerException);
}
