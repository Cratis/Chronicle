// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// The durable owner has not admitted, or has revoked, the writer grant of a generation.
/// </summary>
public sealed class GenerationWriterGrantNotAdmitted() : InvalidOperationException("The generation has no admitted writer grant; only the durable owner can issue one.");
