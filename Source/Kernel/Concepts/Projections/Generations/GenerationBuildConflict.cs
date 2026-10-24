// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// The operation or one of its immutable claims already belongs to different inputs.
/// </summary>
public sealed class GenerationBuildConflict() : Exception("The generation build reservation conflicts with an existing claim.");
