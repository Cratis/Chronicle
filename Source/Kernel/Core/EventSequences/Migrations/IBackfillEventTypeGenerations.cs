// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Defines the add-only job placed only on silos supporting this grain type.
/// </summary>
public interface IBackfillEventTypeGenerations : IJob<BackfillEventTypeGenerationsRequest>;
