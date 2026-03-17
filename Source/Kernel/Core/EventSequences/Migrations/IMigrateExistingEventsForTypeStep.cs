// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Defines a job step that iterates existing events of a specific event type
/// and updates their generational content using the current migration definitions.
/// </summary>
public interface IMigrateExistingEventsForTypeStep : IJobStep<MigrateExistingEventsForTypeRequest, object, MigrateExistingEventsForTypeStepState>;
