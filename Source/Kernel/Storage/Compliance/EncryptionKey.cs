// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819

namespace Aksio.Cratis.Kernel.Storage.Compliance;

/// <summary>
/// Represents a key used in encryption.
/// </summary>
/// <param name="Public">The public part of the key.</param>
/// <param name="Private">The private part of the key.</param>
public record EncryptionKey(byte[] Public, byte[] Private);
