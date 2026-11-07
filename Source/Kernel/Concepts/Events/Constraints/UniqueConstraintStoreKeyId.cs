// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Identifies a unique-constraint store key without revealing its secret material.
/// </summary>
/// <param name="Value">The 128-bit random identifier as 32 lowercase hexadecimal characters.</param>
public sealed record UniqueConstraintStoreKeyId(string Value) : ConceptAs<string>(Value);
