// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_the_text_has_a_lone_surrogate : given.key_names
{
    Exception? _highSurrogateError;
    Exception? _lowSurrogateError;
    Exception? _inEventStoreError;

    void Because()
    {
        _highSurrogateError = Catch.Exception(() => PrefixOf("store", "default", "\uD800"));
        _lowSurrogateError = Catch.Exception(() => PrefixOf("store", "default", "a\uDC00"));
        _inEventStoreError = Catch.Exception(() => PrefixOf("\uD800", "default", "id"));
    }

    [Fact] void should_refuse_a_lone_high_surrogate() => _highSurrogateError.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_a_lone_low_surrogate() => _lowSurrogateError.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_a_lone_surrogate_in_the_event_store() => _inEventStoreError.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
}
