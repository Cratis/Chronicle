// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the request for the job that validates and commits an import.
/// </summary>
/// <param name="ImportId">The import to commit; the event store and namespace are the job's own.</param>
public record ImportEventsRequest(ImportId ImportId) : IJobRequest;
