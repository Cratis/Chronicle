// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A read model carrying the host of a vibe, used to exercise read-model handler-method parameters in the
/// ReactorScenario specs. The projection makes it discoverable; the specs seed its instance directly.
/// </summary>
/// <param name="Id">The vibe identifier.</param>
/// <param name="Host">The host of the vibe.</param>
[Passive]
[FromEvent<VibeStarted>]
public record VibeAttendees([Key] string Id, string Host);
