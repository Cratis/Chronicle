// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents the state of an add-only backfill job, separate from legacy migration jobs.
/// </summary>
public class BackfillEventTypeGenerationsState : JobState;
