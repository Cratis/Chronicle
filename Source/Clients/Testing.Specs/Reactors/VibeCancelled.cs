// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test event representing a vibe being cancelled, used to trigger the <see cref="VibeCancellationReactor"/> in the ReactorScenario specs.
/// </summary>
[EventType]
public record VibeCancelled();
