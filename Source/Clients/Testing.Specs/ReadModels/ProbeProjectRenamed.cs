// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>Renames a project, clearing the nested information.</summary>
/// <param name="ProjectId">The project identifier.</param>
[EventType]
public record ProbeProjectRenamed(Guid ProjectId);
