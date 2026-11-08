// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Defines the step that validates and commits an import, chunk by chunk.
/// </summary>
public interface IImportEventsStep : IJobStep<ImportEventsRequest, object, ImportEventsStepState>;
