// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Recording another request would exceed the inline request log capacity.
/// </summary>
/// <param name="identifier">The key identifier.</param>
public class EncryptionKeyRequestLogFull(EncryptionKeyIdentifier identifier)
    : Exception($"The encryption key erasure request log for identifier '{identifier}' is full. Compact confirmed requests before recording another erasure.");
