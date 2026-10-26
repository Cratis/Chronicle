// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_EventStoreStorage;

public class when_the_provider_has_not_implemented_key_storage : Specification
{
    Exception? _error;

    void Because()
    {
        IEventStoreStorage storage = new given.a_provider_without_key_storage();
        _error = Catch.Exception(() => _ = storage.UniqueConstraintEncoding);
    }

    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<UniqueConstraintEncodingStorageNotSupported>();
}
