// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an <see cref="IdempotencyKey"/> is empty or longer than <see cref="IdempotencyKey.MaximumLength"/> characters.
/// </summary>
public class IdempotencyKeyNotValid()
    : Exception($"An idempotency key must be between 1 and {IdempotencyKey.MaximumLength} characters.");
