// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.given;

public class a_plain_migration : Specification
{
    protected IEventTypes _eventTypes;
    protected IServiceProvider _services;
    protected IActivation _activation;
    protected IEventTypeMigration _migration;
    private protected ExplicitMigrationMethods _methods;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.GetEventTypeFor(typeof(Previous)).Returns(new EventType("shared", 1));
        _eventTypes.GetEventTypeFor(typeof(Upgrade)).Returns(new EventType("shared", 2));
        _activation = Substitute.For<IActivation>();
        _services = new ServiceCollection()
            .AddSingleton(_activation)
            .AddSingleton(Substitute.For<IEventTypes>())
            .BuildServiceProvider();
        _methods = ExplicitMigrationMethods.For(typeof(PlainMigration));
    }

    public interface IActivation
    {
        void Record(IEventTypes eventTypes);
    }

    public record Previous(string OldName, string Status);

    public record Upgrade(string NewName, string Status);

    public class PlainMigration
    {
        public PlainMigration(IActivation activation, IEventTypes eventTypes) => activation.Record(eventTypes);

        public void Upcast(IEventMigrationBuilder<Upgrade, Previous> builder) =>
            builder.Properties(_ => _.RenamedFrom(upgrade => upgrade.NewName, previous => previous.OldName));

        public void Downcast(IEventMigrationBuilder<Previous, Upgrade> builder) =>
            builder.Properties(_ => _.RenamedFrom(previous => previous.OldName, upgrade => upgrade.NewName));

        public void MapValues(IEventValueMapBuilder<Upgrade, Previous> builder) =>
            builder.For(upgrade => upgrade.Status, previous => previous.Status, _ => _.Map("old", "new"));
    }
}
