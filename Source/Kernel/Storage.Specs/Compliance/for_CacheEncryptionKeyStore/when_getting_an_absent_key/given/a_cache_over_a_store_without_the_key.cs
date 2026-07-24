// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_getting_an_absent_key.given;

public class a_cache_over_a_store_without_the_key : Specification
{
    protected static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(30);
    protected static readonly EncryptionKeyIdentifier _identifier = "5c6cce36-d60d-46db-9db2-e820559962db";

    protected CacheEncryptionKeyStorage _store;
    protected IEncryptionKeyStorage _actualStore;
    protected ControllableTimeProvider _timeProvider;

    void Establish()
    {
        _actualStore = Substitute.For<IEncryptionKeyStorage>();
        _timeProvider = new ControllableTimeProvider();
        _store = new(_actualStore, _timeProvider, _timeToLive);
    }
}
