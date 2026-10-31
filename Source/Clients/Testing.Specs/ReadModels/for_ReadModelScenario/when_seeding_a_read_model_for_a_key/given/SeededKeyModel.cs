// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

[FromEvent<SeededKeyRenamed>]
public record SeededKeyModel(
    string Id,
    [SetFrom<SeededKeyRenamed>(nameof(SeededKeyRenamed.Caption))] string Name,
    string Untouched,
    [Increment<SeededKeyRenamed>] int Count);
