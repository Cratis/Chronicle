// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when provisioning an <see cref="EncryptionKey"/> lost the race for a revision to
/// another provisioner, but the key that won could not be read back.
/// </summary>
/// <remarks>
/// The winning key was removed between being written and being read — most likely by a concurrent erasure. The
/// only safe answer is to refuse: returning the losing candidate would hand out key material that no store holds,
/// so anything encrypted under it could never be decrypted, and it would also slip past the erasure that removed
/// the winner. Retrying the operation provisions against whatever state the store is in now.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> that was being provisioned.</param>
/// <param name="revision">The <see cref="EncryptionKeyRevision"/> that was contended.</param>
public class EncryptionKeyProvisioningConflict(EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision)
    : Exception($"Another provisioner claimed revision '{revision}' of the encryption key for identifier '{identifier}', but that key could not be read back - it was most likely removed concurrently. No key was returned; retry the operation.");
