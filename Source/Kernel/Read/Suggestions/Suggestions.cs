// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Grains.Suggestions;
using Aksio.Cratis.Kernel.Persistence.Suggestions;
using Aksio.Cratis.Kernel.Suggestions;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Suggestions;

/// <summary>
/// Represents the API for working with suggestions.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/suggestions")]
public class Suggestions : ControllerBase
{
    readonly ProviderFor<ISuggestionStorage> _suggestionStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Suggestions"/> class.
    /// </summary>
    /// <param name="suggestionStorageProvider">Provider for <see cref="ISuggestionStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Suggestions(
        ProviderFor<ISuggestionStorage> suggestionStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _suggestionStorageProvider = suggestionStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Collection of <see cref="SuggestionInformation"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<SuggestionInformation>> GetSuggestions(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);
        var suggestions = await _suggestionStorageProvider().GetSuggestions();
        return Convert(suggestions);
    }

    /// <summary>
    /// Get and observe all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Client observable of a collection of <see cref="SuggestionInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<SuggestionInformation>>> AllSuggestions(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);

        var clientObservable = new ClientObservable<IEnumerable<SuggestionInformation>>();
        var observable = _suggestionStorageProvider().ObserveSuggestions();
        var subscription = observable.Subscribe(suggestions => clientObservable.OnNext(Convert(suggestions)));
        clientObservable.ClientDisconnected = () =>
        {
            subscription.Dispose();
            if (observable is IDisposable disposableObservable)
            {
                disposableObservable.Dispose();
            }
        };

        return Task.FromResult(clientObservable);
    }

    IEnumerable<SuggestionInformation> Convert(IEnumerable<SuggestionState> suggestions) =>
         suggestions.Select(_ => new SuggestionInformation(_.Id, _.Name, _.Description, _.Type, _.Occurred)).ToArray();
}
