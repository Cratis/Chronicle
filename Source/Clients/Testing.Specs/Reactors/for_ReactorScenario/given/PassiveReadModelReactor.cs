// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records a passive read-model handler parameter.
/// </summary>
/// <param name="recorder">The argument recorder.</param>
public class PassiveReadModelReactor(IRecordScenarioReadModel recorder) : IReactor
{
    /// <summary>
    /// Records the passive model resolved for a started vibe.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <param name="readModel">The resolved read model or null.</param>
    public void Handle(VibeStarted @event, VibeAttendees? readModel) => recorder.Record(readModel);
}
