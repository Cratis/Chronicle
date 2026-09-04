// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Defines the contract for working with observers.
/// </summary>
[Service]
public interface IObservers
{
    /// <summary>
    /// Rewind an observer.
    /// </summary>
    /// <param name="command">The replay command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="ReplayResponse"/> containing the identifier of the replay job that was started or resumed.</returns>
    [Operation]
    Task<ReplayResponse> Replay(Replay command, CallContext context = default);

    /// <summary>
    /// Rewind a partition for an observer.
    /// </summary>
    /// <param name="command">The replay command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ReplayPartition(ReplayPartition command, CallContext context = default);

    /// <summary>
    /// Retry a failed partition for an observer.
    /// </summary>
    /// <param name="command">The retry command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task RetryPartition(RetryPartition command, CallContext context = default);

    /// <summary>
    /// Clear quarantine for an observer.
    /// </summary>
    /// <param name="command">The clear quarantine command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ClearObserverQuarantine(ClearObserverQuarantine command, CallContext context = default);

    /// <summary>
    /// Get the current details of an observer.
    /// </summary>
    /// <param name="request">The <see cref="GetObserverInformationRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="ObserverInformation"/>.</returns>
    Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default);

    /// <summary>
    /// Get the connected clients an observer's subscription delivers events to. Only client-owned
    /// observers have connected clients; for kernel-owned observers the result is empty.
    /// </summary>
    /// <param name="request">The <see cref="GetConnectedClientsForObserverRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="Clients.ConnectedClient"/>.</returns>
    [Operation]
    Task<IEnumerable<Clients.ConnectedClient>> GetConnectedClientsForObserver(GetConnectedClientsForObserverRequest request, CallContext context = default);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ObserverInformation"/>.</returns>
    [Operation]
    Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default);

    /// <summary>
    /// Observe all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [Operation]
    IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default);

    /// <summary>
    /// Waits for all affected observers to complete for a specific append tail sequence number.
    /// </summary>
    /// <param name="request">The <see cref="WaitForObserverCompletionRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="WaitForObserverCompletionResponse"/> describing completion and failures.</returns>
    /// <remarks>
    /// The wait is bounded by the cancellation token on <paramref name="context"/>.
    /// </remarks>
    Task<WaitForObserverCompletionResponse> WaitForCompletion(WaitForObserverCompletionRequest request, CallContext context = default);

    /// <summary>
    /// Checks whether a named set of observers have durably applied through a target position.
    /// </summary>
    /// <param name="request">The <see cref="AppliedThroughRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An <see cref="AppliedThroughResponse"/> carrying a typed outcome per requested observer.</returns>
    /// <remarks>
    /// Unlike <see cref="WaitForCompletion"/>, the observer set is always explicit, never "every observer on the
    /// sequence" implicitly, and a caller's deadline (via <paramref name="context"/>) is reported back as a typed
    /// <see cref="AppliedThroughOutcome.TimedOut"/> per still-unresolved observer rather than an unstructured
    /// cancellation fault - this never reports success from wall-clock timestamps or polling read-model contents,
    /// only from durably observed observer state.
    /// </remarks>
    Task<AppliedThroughResponse> AppliedThrough(AppliedThroughRequest request, CallContext context = default);

    /// <summary>
    /// Get all replayable observers for specific event types.
    /// </summary>
    /// <param name="request">The <see cref="GetReplayableObserversForEventTypesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ObserverInformation"/> for observers that support replay and observe the given event types.</returns>
    [Operation]
    Task<IEnumerable<ObserverInformation>> GetReplayableObserversForEventTypes(GetReplayableObserversForEventTypesRequest request, CallContext context = default);
}
