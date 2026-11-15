// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_a_component_is_empty : given.key_names
{
    Exception? _eventStoreError;
    Exception? _namespaceError;
    Exception? _identifierError;

    void Because()
    {
        _eventStoreError = Catch.Exception(() => PrefixOf(string.Empty, "default", "id"));
        _namespaceError = Catch.Exception(() => PrefixOf("store", string.Empty, "id"));
        _identifierError = Catch.Exception(() => PrefixOf("store", "default", string.Empty));
    }

    [Fact] void should_refuse_an_empty_event_store() => _eventStoreError.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_an_empty_namespace() => _namespaceError.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_an_empty_identifier() => _identifierError.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
}
