// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Defines the contract for working with external services.
/// </summary>
[Service]
public interface IExternalServices
{
    /// <summary>
    /// Add external services.
    /// </summary>
    /// <param name="request">The <see cref="AddExternalServices"/> holding the definitions.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Add(AddExternalServices request, CallContext context = default);

    /// <summary>
    /// Remove external services.
    /// </summary>
    /// <param name="request">The <see cref="RemoveExternalServices"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Remove(RemoveExternalServices request, CallContext context = default);

    /// <summary>
    /// Gets all external services.
    /// </summary>
    /// <param name="request"><see cref="GetExternalServicesRequest"/>.</param>
    /// <returns><see cref="IEnumerable{T}"/> of <see cref="ExternalServiceDefinition"/>.</returns>
    [Operation]
    Task<IEnumerable<ExternalServiceDefinition>> GetExternalServices(GetExternalServicesRequest request);

    /// <summary>
    /// Gets observer over all external services.
    /// </summary>
    /// <param name="request"><see cref="GetExternalServicesRequest"/>.</param>
    /// <param name="context"><see cref="CallContext"/>.</param>
    /// <returns><see cref="IObservable{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="ExternalServiceDefinition"/>.</returns>
    [Operation]
    IObservable<IEnumerable<ExternalServiceDefinition>> ObserveExternalServices(GetExternalServicesRequest request, CallContext context = default);
}
