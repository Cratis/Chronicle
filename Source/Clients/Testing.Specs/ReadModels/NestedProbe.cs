// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>Read model used to verify nested object re-creation after removal.</summary>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="Name">The root name.</param>
/// <param name="Info">The optional nested info.</param>
[Passive]
public record NestedProbe([Key] Guid ProjectId, string Name, ProjectInfo? Info);
