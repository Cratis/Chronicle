// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event opening a thing — an explicitly-subscribed event for the FromAll harness specs.
/// </summary>
/// <param name="Name">The thing's name.</param>
[EventType]
public record ThingOpened(string Name);
