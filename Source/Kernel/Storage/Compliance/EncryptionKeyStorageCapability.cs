// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>Non-sensitive startup diagnostics for one member of a storage composition.</summary>
/// <param name="Type">The provider type, never a key identifier or secret location.</param>
/// <param name="Guarantees">The provider's actual guarantees.</param>
public record EncryptionKeyStorageCapability(string Type, EncryptionKeyFenceGuarantees Guarantees);
