// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_round_tripping_non_string_security_values;

/// <summary>
/// Records confidential values with non-string schema types.
/// </summary>
/// <param name="Count">The confidential integer.</param>
/// <param name="Flag">The confidential boolean.</param>
[EventType]
public record SecretValuesRecorded(SecretCount Count, SecretFlag Flag);
