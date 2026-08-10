// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model used to verify that an empty subscribed changeset retains an explicit initial state.
/// </summary>
/// <param name="Id">Read model identifier.</param>
/// <param name="Name">Name held by the initial state.</param>
[Passive]
[FromEvent<InitialStateRetainingEvent>]
public sealed record InitialStateRetainingReadModel(
    [Key] Guid Id,
    string Name);
