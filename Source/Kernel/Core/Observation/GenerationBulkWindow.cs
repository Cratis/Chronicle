// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The one execution and writer grant a bulk window captured when it began.
/// </summary>
/// <param name="Execution">The captured <see cref="GenerationExecution"/>.</param>
/// <param name="Context">The admitted <see cref="GenerationSinkContext"/>.</param>
internal sealed record GenerationBulkWindow(GenerationExecution Execution, GenerationSinkContext Context);
