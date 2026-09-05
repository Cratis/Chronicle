// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Cuts;

/// <summary>
/// Defines the contract for capturing read-model payloads exactly at an event-sequence cut.
/// </summary>
[Service]
public interface IReadModelCuts
{
    /// <summary>
    /// Captures a selection of read models exactly at a vector of event-sequence cuts.
    /// </summary>
    /// <param name="request">The <see cref="ReadModelCutRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The published <see cref="ReadModelCutResponse"/>.</returns>
    /// <remarks>
    /// Recomputes each requested read model's payload from the event log up to and including its cut, rather
    /// than reading the live sink - so a writer that advances a read model past the requested cut after the
    /// request started never contaminates the payload. The same request, field for field, always resolves to
    /// the same manifest.
    /// </remarks>
    [Operation]
    Task<ReadModelCutResponse> Capture(ReadModelCutRequest request, CallContext context = default);
}
