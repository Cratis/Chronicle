// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// The current module of a topic.
/// </summary>
/// <param name="Id">The topic identity.</param>
/// <param name="Module">The module.</param>
[FromEvent<MigrationTopicCreated>]
public record MigrationTopic(Guid Id, string Module);
