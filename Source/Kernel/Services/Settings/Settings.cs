// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Concepts.Settings;
using Cratis.Chronicle.Contracts.Settings;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Services.Settings;

/// <summary>
/// Represents an implementation of <see cref="ISettings"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
internal sealed class Settings(IGrainFactory grainFactory) : ISettings
{
    /// <inheritdoc/>
    public async Task ConfigureLanguageModel(ConfigureLanguageModel command)
    {
        var endpoint = new Uri(command.Endpoint);
        var apiKey = new ApiKey(command.ApiKey);
        var model = new LanguageModel(command.Model);

        var @event = new Grains.Settings.OpenAIModelConfigured(endpoint, apiKey, model);
        var eventLog = grainFactory.GetEventLog();
        await eventLog.Append("settings", @event);
    }
}
