// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// A keyed digest and a non-secret reference to the exact encryption key revision.
/// </summary>
/// <param name="Kind">The stable protection kind.</param>
/// <param name="KeyReference">The public key fingerprint.</param>
/// <param name="Revision">The exact encrypting revision.</param>
/// <param name="Hash">The lowercase HMAC digest.</param>
internal record ProtectedFingerprintPart(ProtectionKind Kind, string KeyReference, EncryptionKeyRevision Revision, string Hash);
