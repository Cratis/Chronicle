// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_migrating_a_concept_default.given;

/// <summary>
/// A topic was created before module tracking existed.
/// </summary>
[EventTypeGenerationFor<ModuleTopicCreated>(1)]
public record ModuleTopicCreatedV1;
