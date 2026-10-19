// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Holds the validation rules for idempotency keys on keyed append commands.
/// </summary>
internal static class IdempotencyKeyRules
{
    /// <summary>
    /// Requires an idempotency key, when present, to hold something other than whitespace and to fit the supported length.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options.</returns>
    public static IRuleBuilderOptions<T, string?> ValidIdempotencyKey<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(key => string.IsNullOrEmpty(key) || !string.IsNullOrWhiteSpace(key))
            .WithMessage("Idempotency key must not be only whitespace.")
            .Must(key => key is null || key.Length <= IdempotencyKey.MaxLength)
            .WithMessage($"Idempotency key must not exceed {IdempotencyKey.MaxLength} characters.");

    /// <summary>
    /// Requires an idempotency key of a supported length.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options.</returns>
    public static IRuleBuilderOptions<T, string?> RequiredIdempotencyKey<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty().WithMessage("Idempotency key is required.").ValidIdempotencyKey();
}
