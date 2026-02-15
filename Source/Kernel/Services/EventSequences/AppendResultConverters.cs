// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Services.Events.Constraints;
using Cratis.Chronicle.Services.EventSequences.Concurrency;

namespace Cratis.Chronicle.Services.EventSequences;

/// <summary>
/// Represents methods for converting between <see cref="AppendResult"/> and <see cref="AppendResponse"/>.
/// </summary>
internal static class AppendResultConverters
{
    /// <summary>
    /// Convert from a Chronicle representation of <see cref="AppendResult"/> to <see cref="AppendResponse"/>.
    /// </summary>
    /// <param name="result"><see cref="AppendResult"/> to convert from.</param>
    /// <returns>A converted <see cref="AppendResponse"/>.</returns>
    public static AppendResponse ToContract(this AppendResult result) =>
        new()
        {
            CorrelationId = result.CorrelationId,
            SequenceNumber = result.SequenceNumber,
            ConstraintViolations = result.ConstraintViolations.Select(_ => _.ToContract()).ToList(),
            Errors = result.Errors.Select(_ => _.Value).ToList(),
            ConcurrencyViolation = result.ConcurrencyViolation?.ToContract()
        };

    /// <summary>
    /// Convert from a Chronicle representation of <see cref="AppendManyResult"/> to <see cref="AppendManyResponse"/>.
    /// </summary>
    /// <param name="result"><see cref="AppendManyResult"/> to <see cref="AppendManyResponse"/>.</param>
    /// <returns>Converted <see cref="AppendManyResponse"/>.</returns>
    public static AppendManyResponse ToContract(this AppendManyResult result) =>
        new()
        {
            CorrelationId = result.CorrelationId,
            SequenceNumbers = result.SequenceNumbers.Select(_ => _.Value).ToList(),
            ConstraintViolations = result.ConstraintViolations.Select(_ => _.ToContract()).ToList(),
            Errors = result.Errors.Select(_ => _.Value).ToList(),
            ConcurrencyViolations = result.ConcurrencyViolations.Select(_ => _.ToContract()).ToList()
        };
}
