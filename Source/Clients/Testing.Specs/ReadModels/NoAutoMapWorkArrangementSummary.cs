// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with class-level <see cref="NoAutoMapAttribute"/>: name-based AutoMap is fully disabled, so
/// only explicitly-sourced properties are populated. Used to verify that disabling AutoMap still works
/// alongside the explicit-beats-implicit merge — <see cref="Location"/> is set from its explicit source
/// while <see cref="WorkMode"/> (a name match with the event) stays unset.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Location">The location, explicitly sourced.</param>
/// <param name="WorkMode">Name-matches the event property but is left unset because AutoMap is disabled.</param>
[Passive]
[NoAutoMap]
[FromEvent<WorkArrangementSet>]
public record NoAutoMapWorkArrangementSummary(
    [Key] Guid Id,

    [SetFrom<WorkArrangementSet>(nameof(WorkArrangementSet.Location))]
    string Location,

    int WorkMode);
