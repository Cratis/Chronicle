// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// The exception that is thrown when a unique-constraint store key identity is not in its canonical form.
/// </summary>
/// <param name="length">The length of the rejected identity, which is the only detail revealed.</param>
public sealed class InvalidUniqueConstraintStoreKeyId(int length)
    : Exception($"Unique-constraint store key id must be 32 lowercase hexadecimal characters (16 bytes), but had length {length}.");
