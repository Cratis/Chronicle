// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers;

/// <summary>
/// Represents the result of performing work.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public record PerformWorkResult<TResult>
{
    /// <summary>
    /// The optional result. This can be present even if there was an <see cref="PerformWorkError"/>.
    /// </summary>
    public TResult? Result { get; init; }

    /// <summary>
    /// The <see cref="PerformWorkError"/>.
    /// </summary>
    public PerformWorkError Error { get; init; }

    /// <summary>
    /// The optional <see cref="Exception"/>.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets a value indicating whether there is a result.
    /// </summary>
    public bool HasResult => Result is not null;

    /// <summary>
    /// Gets a value indicating whether there is an exception.
    /// </summary>
    public bool HasException => Exception is not null;

    /// <summary>
    /// Gets a value indicating whether the performing of the work was completely successful.
    /// </summary>
    public bool IsSuccess => Error == PerformWorkError.None && !HasException;
}
