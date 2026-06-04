// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;
using ContractCompleteStreamError = Cratis.Chronicle.Contracts.EventSequences.CompleteStreamError;
using ContractCompleteStreamResponse = Cratis.Chronicle.Contracts.EventSequences.CompleteStreamResponse;

namespace Cratis.Chronicle.Services.EventSequences;

/// <summary>
/// Conversion helpers between the Chronicle representation of stream completion and the wire-level contract types.
/// </summary>
internal static class CompleteStreamConverters
{
    /// <summary>
    /// Convert a <see cref="Result{TSuccess, TError}"/> from the grain into a <see cref="ContractCompleteStreamResponse"/>.
    /// </summary>
    /// <param name="result">The result returned from the grain.</param>
    /// <returns>A <see cref="ContractCompleteStreamResponse"/> ready to send back to the caller.</returns>
    public static ContractCompleteStreamResponse ToContract(this Result<EventSequenceNumber, CompleteStreamError> result)
    {
        if (result.TryGetError(out var error))
        {
            return new ContractCompleteStreamResponse
            {
                IsSuccess = false,
                Error = error.ToContract()
            };
        }

        return new ContractCompleteStreamResponse
        {
            IsSuccess = true,
            SequenceNumber = result.AsT0
        };
    }

    /// <summary>
    /// Convert a Chronicle <see cref="CompleteStreamError"/> to its contract counterpart.
    /// </summary>
    /// <param name="error">The Chronicle <see cref="CompleteStreamError"/>.</param>
    /// <returns>The wire-level <see cref="ContractCompleteStreamError"/>.</returns>
    public static ContractCompleteStreamError ToContract(this CompleteStreamError error) =>
        error switch
        {
            CompleteStreamError.AlreadyCompleted => ContractCompleteStreamError.AlreadyCompleted,
            CompleteStreamError.DefaultStreamCannotBeCompleted => ContractCompleteStreamError.DefaultStreamCannotBeCompleted,
            _ => ContractCompleteStreamError.AlreadyCompleted
        };
}
