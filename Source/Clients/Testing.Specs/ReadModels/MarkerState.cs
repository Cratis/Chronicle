// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model created by an event that carries no properties, so the event maps nothing onto it.
/// </summary>
/// <param name="Id">Read model identifier.</param>
/// <param name="IsMarked">Whether the instance is marked, left at its default by the event.</param>
[Passive]
[FromEvent<Marked>]
public record MarkerState(Guid Id, bool IsMarked = false);
