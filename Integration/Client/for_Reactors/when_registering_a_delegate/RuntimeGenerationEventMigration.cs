// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_Reactors.when_registering_a_delegate;

public class RuntimeGenerationEventMigration : EventTypeMigration<RuntimeGenerationEvent, RuntimeGenerationEventV1>
{
    public override void Upcast(IEventMigrationBuilder<RuntimeGenerationEvent, RuntimeGenerationEventV1> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Number, source => source.Number));

    public override void Downcast(IEventMigrationBuilder<RuntimeGenerationEventV1, RuntimeGenerationEvent> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Number, source => source.Number));
}
