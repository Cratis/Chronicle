// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>A published version in an external key-material service.</summary>
/// <param name="Revision">The key revision.</param>
/// <param name="Name">The secret name.</param>
/// <param name="Version">The exact secret version.</param>
/// <param name="Lifecycle">The immutable staged lifecycle.</param>
/// <param name="Fingerprint">The material fingerprint.</param>
/// <param name="Deleted">Whether a raw deletion hid this revision without recording an erasure.</param>
public record EncryptionKeyPublication(uint Revision, string Name, string Version, uint Lifecycle, string Fingerprint, bool Deleted = false);
