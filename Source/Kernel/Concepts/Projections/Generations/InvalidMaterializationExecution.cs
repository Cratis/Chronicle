// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// The immutable execution document is missing, conflicting or uninterpretable.
/// </summary>
public sealed class InvalidMaterializationExecution() : Exception("The generation execution specification is missing, conflicting or unsupported.");
