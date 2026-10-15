// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A topic was created in a module.
/// </summary>
/// <param name="Module">The module.</param>
[EventType(generation: 2)]
public record MigrationTopicCreated(string Module);
