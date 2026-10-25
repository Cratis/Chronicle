// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// A verified generation binding with its immutable execution snapshot and the engine projection built from it.
/// </summary>
/// <param name="Binding">The verified observer binding.</param>
/// <param name="Execution">The persisted execution snapshot.</param>
/// <param name="Inputs">The inputs restored from the snapshot.</param>
/// <param name="Projection">The engine projection built from the snapshot, never from the current definition.</param>
public sealed record GenerationExecution(
    GenerationObserverBinding Binding,
    MaterializationExecutionSpecification Execution,
    MaterializationExecutionInputs Inputs,
    EngineProjection Projection);
