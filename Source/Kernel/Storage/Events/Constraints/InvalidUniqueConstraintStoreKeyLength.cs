// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// The exception that is thrown when unique-constraint key material is not exactly 256 bits.
/// </summary>
public sealed class InvalidUniqueConstraintStoreKeyLength() : Exception("Unique-constraint store key material must be exactly 32 bytes.");
