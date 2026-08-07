// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_checking_if_a_key_exists;

public class and_no_store_has_it : given.two_key_stores
{
    bool _result;
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => _result = await _composite.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));

    [Fact] void should_report_that_it_does_not_exist() => _result.ShouldBeFalse();
    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
