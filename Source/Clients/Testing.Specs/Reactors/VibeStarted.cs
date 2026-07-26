// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test event representing a vibe being started, used to build the <see cref="VibeAttendees"/> read model in the ReactorScenario specs.
/// </summary>
/// <param name="Host">The host of the vibe.</param>
[EventType]
public record VibeStarted(string Host);
