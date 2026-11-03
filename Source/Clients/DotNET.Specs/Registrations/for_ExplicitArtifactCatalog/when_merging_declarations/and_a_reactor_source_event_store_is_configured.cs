// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_a_reactor_source_event_store_is_configured : given.a_catalog
{
    Type _plain;
    Type _marked;
    Type _unconfigured;

    void Establish()
    {
        _store = "host-product";
        var configuredName = _store.Value;
        _plain = EmitType("PlainReactor");
        _marked = EmitType("MarkedReactor", [AttributeFor<EventStoreAttribute>([typeof(string)], configuredName)]);
        _unconfigured = EmitType("UnconfiguredReactor");
        _options.RegisterReactor(_plain, ReactorRegistrationOptions.FromEventStore(configuredName))
            .RegisterReactor(_marked, ReactorRegistrationOptions.FromEventStore(configuredName))
            .RegisterReactor(_unconfigured);
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_resolve_the_configured_store() => ExplicitArtifactCatalog.From(_catalog)!.GetReactorSourceStoreFor(_plain).ShouldEqual((EventStoreName)"host-product");
    [Fact] void should_accept_an_equal_attribute() => ExplicitArtifactCatalog.From(_catalog)!.GetReactorSourceStoreFor(_marked).ShouldEqual((EventStoreName)"host-product");
    [Fact] void should_leave_unconfigured_registrations_unchanged() => ExplicitArtifactCatalog.From(_catalog)!.GetReactorSourceStoreFor(_unconfigured).ShouldBeNull();
}
