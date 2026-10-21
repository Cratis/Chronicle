// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records a non-nullable read-model handler parameter.
/// </summary>
/// <param name="recorder">The argument recorder.</param>
public class NonNullableReadModelReactor(IRecordScenarioReadModel recorder) : IReactor
{
    /// <summary>
    /// Records the model resolved for a started vibe.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <param name="readModel">The resolved read model.</param>
    public void Handle(VibeStarted @event, ScenarioReadModel readModel) => recorder.Record(readModel);
}
