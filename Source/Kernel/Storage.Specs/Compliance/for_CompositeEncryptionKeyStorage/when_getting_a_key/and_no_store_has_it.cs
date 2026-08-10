// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_no_store_has_it : given.two_key_stores
{
    EncryptionKey? _result;
    Exception _error;
    bool _primaryHasIt;
    bool _secondaryHasIt;

    async Task Because()
    {
        _error = await Catch.Exception(async () => _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _primaryHasIt = await HasKeyIn(_primary);
        _secondaryHasIt = await HasKeyIn(_secondary);
    }

    [Fact] void should_report_the_key_as_absent() => _result.ShouldBeNull();
    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_write_anything_into_the_primary_store() => _primaryHasIt.ShouldBeFalse();
    [Fact] void should_not_write_anything_into_the_secondary_store() => _secondaryHasIt.ShouldBeFalse();
}
