// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for creating a module.
/// </summary>
/// <param name="Name">Module name.</param>
[EventType]
public record ModuleCreated(string Name);
