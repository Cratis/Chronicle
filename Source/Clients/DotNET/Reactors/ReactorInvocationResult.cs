// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents the result of invoking a reactor, containing both exception state and side-effect failures.
/// </summary>
/// <param name="ExceptionResult">The <see cref="Catch"/> holding any unexpected exceptions that occurred.</param>
/// <param name="SideEffectFailure">Optional <see cref="ReactorSideEffectFailure"/> containing side-effect append failures.</param>
public record ReactorInvocationResult(Catch ExceptionResult, ReactorSideEffectFailure? SideEffectFailure = null)
{
    /// <summary>
    /// Gets a value indicating whether the invocation was successful (no exceptions or side-effect failures).
    /// </summary>
    public bool IsSuccess => !ExceptionResult.TryGetException(out _) && SideEffectFailure is null;

    /// <summary>
    /// Gets a value indicating whether the invocation failed.
    /// </summary>
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Creates a successful invocation result.
    /// </summary>
    /// <returns>A <see cref="ReactorInvocationResult"/> representing success.</returns>
    public static ReactorInvocationResult Success() => new(Catch.Success());

    /// <summary>
    /// Creates an invocation result from an exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A <see cref="ReactorInvocationResult"/> containing the exception.</returns>
    public static ReactorInvocationResult FromException(Exception exception) => new((Catch)exception);

    /// <summary>
    /// Creates an invocation result from a side-effect failure.
    /// </summary>
    /// <param name="failure">The side-effect failure that occurred.</param>
    /// <returns>A <see cref="ReactorInvocationResult"/> containing the failure.</returns>
    public static ReactorInvocationResult FromSideEffectFailure(ReactorSideEffectFailure failure) =>
        new(Catch.Success(), failure);
}
