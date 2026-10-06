// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames.when_resolving_global_confidentiality;

public class and_the_key_uses_the_reserved_event_store : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DatabaseNames.ForEventStore(EncryptedValueKeyIdentifiers.GlobalEventStore));

    [Fact] void should_accept_the_global_key_storage_coordinates() => _exception.ShouldBeNull();
}
