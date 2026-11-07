// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.given;

/// <summary>
/// Helpers for specs of the secret names derived for an encryption key address.
/// </summary>
public class key_names : Specification
{
    protected const string SampleGuid = "3f2a9c1e-7b4d-4e58-9a61-0c2d5e8f1a37";

    protected static string PrefixOf(string eventStore, string eventStoreNamespace, string identifier) =>
        SecretNames.KeyPrefix(eventStore, eventStoreNamespace, identifier);

    protected static string? LegacyOf(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        SecretNames.LegacyPrefix(eventStore, eventStoreNamespace, identifier);

    protected static bool Differ(string[] left, string[] right) =>
        PrefixOf(left[0], left[1], left[2]) != PrefixOf(right[0], right[1], right[2]);
}
