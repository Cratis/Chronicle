// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_from_an_event_at_a_later_generation;

/// <summary>
/// Migrates <see cref="MemberEnrolled"/> between its first and second generation.
/// </summary>
public class MemberEnrolledMigrator : IEventTypeMigrationFor<MemberEnrolled>
{
    /// <inheritdoc/>
    public EventTypeGeneration From => 1;

    /// <inheritdoc/>
    public EventTypeGeneration To => 2;

    /// <inheritdoc/>
    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            pb.Split("FirstName", "FullName", " ", 0);
            pb.Split("LastName", "FullName", " ", 1);
        });

    /// <inheritdoc/>
    public void Downcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.Combine("FullName", " ", "FirstName", "LastName"));
}
