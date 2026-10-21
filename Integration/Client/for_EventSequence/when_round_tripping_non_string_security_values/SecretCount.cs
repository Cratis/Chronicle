// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_round_tripping_non_string_security_values;

/// <summary>
/// A confidential integer value.
/// </summary>
/// <param name="Value">The value to protect.</param>
[Encrypted]
public record SecretCount(int Value) : ConceptAs<int>(Value);
