// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Storage;
using Polly;
using Polly.Registry;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Represents a resilient implementation of <see cref="IGrainStorage"/>.
/// </summary>
/// <param name="storage">The underlying <see cref="IGrainStorage"/>.</param>
/// <param name="resiliencePipelineProvider">The <see cref="ResiliencePipelineProvider{TKey}"/>.</param>
public class ResilientGrainStorage(IGrainStorage storage, ResiliencePipelineProvider<string> resiliencePipelineProvider) : IGrainStorage
{
    /// <summary>
    /// The <see cref="ResiliencePipeline"/> key.
    /// </summary>
    public const string ResiliencePipelineKey = "Retry-Grain-Storage";

    readonly ResiliencePipeline _resiliencePipeline = resiliencePipelineProvider.GetPipeline(ResiliencePipelineKey);

    /// <inheritdoc/>
    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => _resiliencePipeline.ExecuteAsync(async _ =>
        await storage.ReadStateAsync(stateName, grainId, grainState)).AsTask();

    /// <inheritdoc/>
    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => _resiliencePipeline.ExecuteAsync(async _ =>
        await storage.WriteStateAsync(stateName, grainId, grainState)).AsTask();

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => _resiliencePipeline.ExecuteAsync(async _ =>
        await storage.ClearStateAsync(stateName, grainId, grainState)).AsTask();
}
