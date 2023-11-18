// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Suggestions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Persistence.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for <see cref="SuggestionState"/>.
/// </summary>
public class SuggestionGrainStorageProvider : IGrainStorage
{
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public SuggestionGrainStorageProvider(
        IServiceProvider serviceProvider,
        IExecutionContextManager executionContextManager)
    {
        _serviceProvider = serviceProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var suggestionId, out var keyExtension))
        {
            var key = (SuggestionKey)keyExtension!;

            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var suggestionStorage = _serviceProvider.GetRequiredService<ISuggestionStorage>();
            await suggestionStorage.Remove(suggestionId);
        }
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var suggestionId, out var keyExtension))
        {
            var actualGrainState = (grainState as IGrainState<SuggestionState>)!;
            var key = (SuggestionKey)keyExtension!;

            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var suggestionStorage = _serviceProvider.GetRequiredService<ISuggestionStorage>();
            actualGrainState.State = await suggestionStorage.Get(suggestionId) ?? new SuggestionState();
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var suggestionId, out var keyExtension))
        {
            var actualGrainState = (grainState as IGrainState<SuggestionState>)!;
            var key = (SuggestionKey)keyExtension!;

            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var suggestionStorage = _serviceProvider.GetRequiredService<ISuggestionStorage>();
            await suggestionStorage.Save(suggestionId, actualGrainState.State);
        }
    }
}
