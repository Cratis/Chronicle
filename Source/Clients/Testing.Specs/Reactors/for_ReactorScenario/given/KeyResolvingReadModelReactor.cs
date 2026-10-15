// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Resolves a model by a custom key rather than the triggering event source.
/// </summary>
/// <param name="recorder">The argument recorder.</param>
public class KeyResolvingReadModelReactor(IRecordScenarioReadModel recorder) : IReactor, ICanResolveReadModelKey
{
    /// <inheritdoc/>
    public ReadModelKey Resolve(object @event, EventContext context) => ((VibeStarted)@event).Host;

    /// <summary>
    /// Records the model resolved by the custom key.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <param name="readModel">The resolved read model or null.</param>
    public void Handle(VibeStarted @event, ScenarioReadModel? readModel) => recorder.Record(readModel);
}
