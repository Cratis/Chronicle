// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an <see cref="IEncryptionKeyStorage"/> cannot record an erasure.
/// </summary>
/// <remarks>
/// Every key store Chronicle ships records erasures. A custom store written before the erasure fence existed does
/// not, and deleting a key in it leaves exactly the absence a later provisioning reads as "never protected" - so
/// the erasure would be reversible, which is the defect the fence exists to close. Refusing loudly is the safe
/// outcome: an erasure that cannot be fenced is not an erasure, and reporting one would be worse than failing.
/// Implement <see cref="IEncryptionKeyStorage.RecordErasureFor"/>, <see cref="IEncryptionKeyStorage.GetErasureFor"/>
/// and <see cref="IEncryptionKeyStorage.AllowNewKeyFor"/> in the store to resolve it.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> the erasure was being recorded for.</param>
public class EncryptionKeyErasureNotSupported(EncryptionKeyIdentifier identifier)
    : Exception(
        $"The configured encryption key store cannot record the erasure of identifier '{identifier}', so the key could not be erased in a way that stops it being provisioned or copied back. Implement the erasure members of IEncryptionKeyStorage in the store before relying on right-to-erasure.");
