// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Settings;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

namespace Cratis.Chronicle.Grains.Settings;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles settings events.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
[Reactor(eventSequence: WellKnownEventSequences.EventLog)]
public class SettingsReactor(IGrainFactory grainFactory) : Reactor
{
    /// <summary>
    /// Handles when OpenAI model has been configured.
    /// </summary>
    /// <param name="event">The event containing the OpenAI configuration.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task ModelConfigured(OpenAIModelConfigured @event, EventContext eventContext)
    {
        var settings = grainFactory.GetGrain<ISettings>("default");
        var provider = new OpenAIProvider(@event.Endpoint, @event.ApiKey, @event.Model);
        await settings.SetLanguageModelProvider(provider);
    }
}
