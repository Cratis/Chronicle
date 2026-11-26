// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// A typed event containing collections that do not use record value equality.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Values">Ordered values.</param>
/// <param name="Metadata">Named values.</param>
[EventType]
public record AssertionEvent(string Name, IReadOnlyList<int> Values, IReadOnlyDictionary<string, object?> Metadata);
