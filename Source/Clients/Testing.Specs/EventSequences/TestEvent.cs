// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// A test event used by the Testing.Specs project.
/// </summary>
/// <param name="Value">A test value.</param>
[EventType]
public record TestEvent(string Value);
