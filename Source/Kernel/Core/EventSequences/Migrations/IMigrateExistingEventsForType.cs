// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Defines a job for migrating existing stored events when a new event type generation is added.
/// </summary>
public interface IMigrateExistingEventsForType : IJob<MigrateExistingEventsForTypeRequest>;
