// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Defines the job that validates and commits an import.
/// </summary>
public interface IImportEvents : IJob<ImportEventsRequest>;
