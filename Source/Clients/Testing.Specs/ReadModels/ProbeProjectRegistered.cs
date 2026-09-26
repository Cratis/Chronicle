// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>Registers a project with a name.</summary>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="Name">The name.</param>
[EventType]
public record ProbeProjectRegistered(Guid ProjectId, string Name);
