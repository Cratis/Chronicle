// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

/// <summary>
/// A Vault or Azure Key Vault primary migrating from the default storage cannot turn the identifier into a storage
/// location. The default storage must not answer in its place, or a key the primary can never hold would be served.
/// </summary>
public class as_the_primary_that_cannot_address_it : conformance
{
    protected override CompositeEncryptionKeyStorage Compose(given.CapturingLogger<CompositeEncryptionKeyStorage> logger, IEncryptionKeyStorage refusing) =>
        new(logger, refusing, _secondary);

    protected override Exception Refusal() => new InvalidEncryptionKeyAddress("identifier");
}
