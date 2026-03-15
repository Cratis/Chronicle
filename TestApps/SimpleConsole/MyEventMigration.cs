// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace TestApp;

/// <summary>
/// Migrates <see cref="MyEvent"/> between generations 1 and 2.
/// Generation 1 stored the event as a single "subject:body" string in the Message property.
/// Generation 2 (current) splits Message into separate Subject and Body properties.
/// </summary>
public class MyEventMigration : IEventTypeMigrationFor<MyEvent>
{
    /// <inheritdoc/>
    public EventTypeGeneration From => 1;

    /// <inheritdoc/>
    public EventTypeGeneration To => 2;

    /// <inheritdoc/>
    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            pb.Split("Subject", "Message", ":", 0);   // part 0 → Subject
            pb.Split("Body", "Message", ":", 1);      // part 1 → Body
        });

    /// <inheritdoc/>
    public void Downcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.Combine("Message", "Subject", "Body"));
}
