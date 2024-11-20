// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the result of trying to get a single value that can have an optional <see cref="Exception"/> error.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
/// <typeparam name="TErrorType">The error type.</typeparam>
public class SafeTry<T, TErrorType> : OneOfBase<T, ErrorType<TErrorType>>
    where TErrorType : Enum
{
    SafeTry(OneOf<T, ErrorType<TErrorType>> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the try was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator SafeTry<T, TErrorType>(T value) => SafeTry<T, TErrorType>.Success(value);
    public static implicit operator SafeTry<T, TErrorType>(Exception error) => SafeTry<T, TErrorType>.Failed(error);
    public static implicit operator SafeTry<T, TErrorType>(TErrorType errorType) => SafeTry<T, TErrorType>.Failed(errorType);
    public static explicit operator T(SafeTry<T, TErrorType> obj) => obj.AsT0;
    public static explicit operator ErrorType<TErrorType>(SafeTry<T, TErrorType> obj) => obj.AsT1;
    public static explicit operator TErrorType(SafeTry<T, TErrorType> obj) => obj.AsT1.AsT0;
    public static explicit operator Exception(SafeTry<T, TErrorType> obj) => obj.AsT1.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Try{T, TErrorType}"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Try{T, TErrorType}"/>.</returns>
    public static SafeTry<T, TErrorType> Failed(Exception error) => new(OneOf<T, ErrorType<TErrorType>>.FromT1(error));

    /// <summary>
    /// Creates a failed <see cref="Try{T, TErrorType}"/>.
    /// </summary>
    /// <param name="error">The error type.</param>
    /// <returns>The created <see cref="Try{T, TErrorType}"/>.</returns>
    public static SafeTry<T, TErrorType> Failed(TErrorType error) => new(OneOf<T, ErrorType<TErrorType>>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Try{T, TErrorType}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Try{T, TErrorType}"/>.</returns>
    public static SafeTry<T, TErrorType> Success(T value) => new(OneOf<T, ErrorType<TErrorType>>.FromT0(value));

    /// <summary>
    /// Try to get the result <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>A boolean indicating whether the result was present.</returns>
    public bool TryGetResult([NotNullWhen(true)] out T result) => TryPickT0(out result, out _);
}
