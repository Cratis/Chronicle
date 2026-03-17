// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace TestApp;

/// <summary>
/// Migrates <see cref="MyEvent"/> from generation 1 (<see cref="MyEventV1"/>) to generation 2.
/// Generation 1 stored the event as a single "subject:body" string in the Message property.
/// Generation 2 (current) splits Message into separate Subject and Body properties.
/// </summary>
public class MyEventMigration : EventTypeMigration<MyEvent, MyEventV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<MyEvent, MyEventV1> builder) =>
        builder.Properties(pb => pb
            .Split(e => e.Subject, s => s.Message, ":", SplitPartIndex.First)
            .Split(e => e.Body,    s => s.Message, ":", SplitPartIndex.Second));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<MyEventV1, MyEvent> builder) =>
        builder.Properties(pb => pb
            .Combine(e => e.Message, ":", s => s.Subject, s => s.Body));
}
