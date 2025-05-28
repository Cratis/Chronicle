// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents methods for converting between <see cref="AppendResponse"/> to <see cref="AppendResult"/>.
/// </summary>
internal static class AppendResultConverters
{
    /// <summary>
    /// Convert from <see cref="AppendResponse"/> to <see cref="AppendResult"/>.
    /// </summary>
    /// <param name="result"><see cref="AppendResponse"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendResult"/>.</returns>
    public static AppendResult ToClient(this AppendResponse result)
    {
        return new AppendResult
        {
            CorrelationId = result.CorrelationId,
            SequenceNumber = result.SequenceNumber,
            ConstraintViolations = result.ConstraintViolations.Select(v => v.ToClient()).ToImmutableList(),
            Errors = result.Errors.Select(e => (AppendError)e).ToImmutableList()
        };
    }

    /// <summary>
    /// Convert from <see cref="AppendManyResponse"/> to <see cref="AppendManyResult"/>.
    /// </summary>
    /// <param name="result"><see cref="AppendManyResponse"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendManyResult"/>.</returns>
    public static AppendManyResult ToClient(this AppendManyResponse result)
    {
        return new AppendManyResult
        {
            CorrelationId = result.CorrelationId,
            SequenceNumbers = result.SequenceNumbers.Select(_ => (EventSequenceNumber)_).ToImmutableList(),
            ConstraintViolations = result.ConstraintViolations.Select(v => v.ToClient()).ToImmutableList(),
            Errors = result.Errors.Select(e => (AppendError)e).ToImmutableList()
        };
    }
}
