// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Defines the contract for working with captures.
/// </summary>
[Service]
public interface ICaptures
{
    /// <summary>
    /// Save a capture. A capture that is started can not be changed and is rejected with a message.
    /// </summary>
    /// <param name="request">The <see cref="SaveCapture"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="SaveCaptureResponse"/>.</returns>
    [Operation]
    Task<SaveCaptureResponse> Save(SaveCapture request, CallContext context = default);

    /// <summary>
    /// Delete a capture, stopping it first if it is running.
    /// </summary>
    /// <param name="request">The <see cref="DeleteCapture"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Delete(DeleteCapture request, CallContext context = default);

    /// <summary>
    /// Start a capture. Once started it captures on its schedule and can not be changed until stopped.
    /// </summary>
    /// <param name="request">The <see cref="StartCapture"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="StartCaptureResponse"/> - empty messages means the capture was started.</returns>
    [Operation]
    Task<StartCaptureResponse> Start(StartCapture request, CallContext context = default);

    /// <summary>
    /// Stop a capture.
    /// </summary>
    /// <param name="request">The <see cref="StopCapture"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Stop(StopCapture request, CallContext context = default);

    /// <summary>
    /// Gets all captures.
    /// </summary>
    /// <param name="request"><see cref="GetCapturesRequest"/>.</param>
    /// <returns><see cref="IEnumerable{T}"/> of <see cref="Capture"/>.</returns>
    [Operation]
    Task<IEnumerable<Capture>> GetCaptures(GetCapturesRequest request);

    /// <summary>
    /// Gets an observer over all captures.
    /// </summary>
    /// <param name="request"><see cref="GetCapturesRequest"/>.</param>
    /// <param name="context"><see cref="CallContext"/>.</param>
    /// <returns><see cref="IObservable{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="Capture"/>.</returns>
    [Operation]
    IObservable<IEnumerable<Capture>> ObserveCaptures(GetCapturesRequest request, CallContext context = default);

    /// <summary>
    /// Validate a capture declaration - compiling it and verifying what it references, such as
    /// external services and event types.
    /// </summary>
    /// <param name="request">The <see cref="ValidateCaptureDeclaration"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="ValidateCaptureDeclarationResponse"/> - empty messages means the declaration is valid.</returns>
    [Operation]
    Task<ValidateCaptureDeclarationResponse> ValidateDeclaration(ValidateCaptureDeclaration request, CallContext context = default);
}
