// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoders;

public class when_the_load_fails : given.a_loader
{
    static readonly EventStoreName _store = "store";
    Exception _error;
    UniqueConstraintKeyedEncoder? _retried;
    IUniqueConstraintEncodingStorage _keyStorage;

    void Establish()
    {
        _keyStorage = KeyStorageFor(_store, null);
        _keyStorage.GetKey().Returns(
            Task.FromException<UniqueConstraintEncodingKey?>(new InvalidOperationException("unavailable")),
            Task.FromResult<UniqueConstraintEncodingKey?>(UniqueConstraintEncodingKey.Create()));
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () => await _loader.TryGetFor(_store));
        _retried = await _loader.TryGetFor(_store);
    }

    [Fact] void should_surface_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_read_again_after_the_failure() => _keyStorage.Received(2).GetKey();
    [Fact] void should_load_the_encoder_on_retry() => _retried.ShouldNotBeNull();
}
