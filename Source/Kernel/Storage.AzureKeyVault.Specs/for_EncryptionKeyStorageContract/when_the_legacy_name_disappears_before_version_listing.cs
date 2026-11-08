// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_the_legacy_name_disappears_before_version_listing : given.a_fenced_store
{
    Exception? _error;
    EncryptionKey? _result;

    async Task Establish()
    {
        await Client.SetSecretAsync(SecretLocations.For(EventStore, Namespace, Identifier).Name(1u), JsonSerializer.Serialize(Original));
        Client.AfterUnversionedRead = _ =>
        {
            Client.VersionListingNotFound = true;
            return Task.CompletedTask;
        };
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await Storage.TryGetFor(EventStore, Namespace, Identifier));

    [Fact] void should_treat_the_missing_name_as_no_legacy_material() => _result.ShouldBeNull();
    [Fact] void should_not_surface_the_enumeration_race_as_an_erasure_failure() => _error.ShouldBeNull();
}
