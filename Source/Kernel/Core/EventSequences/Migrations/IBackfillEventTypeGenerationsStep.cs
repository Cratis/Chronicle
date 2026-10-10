// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Defines the guarded, add-only generation backfill step.
/// </summary>
public interface IBackfillEventTypeGenerationsStep : IJobStep<BackfillEventTypeGenerationsRequest, object, BackfillEventTypeGenerationsStepState>;
