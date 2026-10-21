// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_binding_to_a_store;

public class and_a_custom_provider_advertises_a_legacy_recipe : given.store_bindings
{
    IEventTypeMigrators _custom;
    IEventTypeMigration[] _output;
    IEnumerable<IEventTypeMigration> _result;

    void Establish()
    {
        // Constructor DI alone must not classify this recipe as registry-bound: it deliberately uses the legacy base constructor.
        var recipe = EmitTypedMigration(typeof(TestEventV2), typeof(TestEventV1), registryPath: false);
        _catalog = new ExplicitArtifacts().RegisterEventType<TestEventV2>().RegisterMigration(recipe).CreateSnapshot().ApplyTo(_discovered);
        _eventTypes.HasFor(typeof(TestEventV2)).Returns(true);
        _custom = Substitute.For<IEventTypeMigrators>();
        _custom.AllMigrators.Returns([recipe]);
        var opaque = Substitute.For<IEventTypeMigration>();
        opaque.From.Returns((EventTypeGeneration)1);
        opaque.To.Returns((EventTypeGeneration)2);
        _output = [opaque];
        _custom.GetMigratorsFor(typeof(TestEventV2)).Returns(_output);
    }

    void Because()
    {
        _bound = StoreEventTypeMigrators.Bind(_custom, _catalog, _eventTypes, _services, new DefaultNamingPolicy());
        _result = _bound.GetMigratorsFor(typeof(TestEventV2));
    }

    [Fact] void should_preserve_opaque_legacy_results_verbatim() => ReferenceEquals(_result, _output).ShouldBeTrue();
    [Fact] void should_not_require_the_omitted_previous_generation() => _eventTypes.DidNotReceive().GetEventTypeFor(typeof(TestEventV1));
}
