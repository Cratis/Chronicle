// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_migrating_a_concept_default.given;

/// <summary>
/// The module containing a topic.
/// </summary>
/// <param name="Value">The module name.</param>
public record ModuleKey(string Value) : ConceptAs<string>(Value);
