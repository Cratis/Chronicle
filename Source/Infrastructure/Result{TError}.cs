// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the result of trying an execution that returns success or a specific error.
/// </summary>
/// <typeparam name="TError">The type of the error.</typeparam>
public class Result<TError> : OneOfBase<None, TError>
{
    /// <summary>
    /// Initializes an instance of the <see cref="Result{TResult}"/> class.
    /// </summary>
    /// <param name="input">The input.</param>
    Result(OneOf<None, TError> input)
        : base(input)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Result<TError>(TError error) => Failed(error);

    public static explicit operator TError(Result<TError> obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TError> Failed(TError error) => new(OneOf<None, TError>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TError> Success() => new(OneOf<None, TError>.FromT0(default));

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>A boolean indicating whether the error was present.</returns>
    public bool TryGetError([NotNullWhen(true)] out TError? error) => TryPickT1(out error, out _);
}
