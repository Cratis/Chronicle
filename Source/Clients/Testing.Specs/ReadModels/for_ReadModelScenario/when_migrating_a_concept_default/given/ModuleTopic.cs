// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_migrating_a_concept_default.given;

/// <summary>
/// The module a topic belongs to after migration.
/// </summary>
/// <param name="Id">The topic identity.</param>
/// <param name="Module">The module.</param>
[FromEvent<ModuleTopicCreated>]
public record ModuleTopic(Guid Id, ModuleKey Module);
