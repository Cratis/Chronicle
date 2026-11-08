// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

/// <summary>
/// A Vault or Azure Key Vault primary migrating from the default storage holds the key under an earlier name that needs
/// the key upgrade. The default storage must not answer in its place and no fresh key may be provisioned.
/// </summary>
public class as_the_primary_holding_the_key_under_an_earlier_name : conformance
{
    protected override CompositeEncryptionKeyStorage Compose(given.CapturingLogger<CompositeEncryptionKeyStorage> logger, IEncryptionKeyStorage refusing) =>
        new(logger, refusing, _secondary);

    protected override Exception Refusal() => new EncryptionKeyNeedsUpgrade();
}
