// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_using_exemptions : Specification
{
    const string ExemptStore = "$kernel$";
    const string ExemptNamespace = "$kernel-namespace$";

    ResourceNameGate _gate;
    IResourceNameReservations _reservations;
    Exception _store;
    Exception _namespace;
    Exception _otherNamespace;
    Exception _created;
    Exception _unused;

    void Establish()
    {
        _reservations = Substitute.For<IResourceNameReservations>();
        _gate = new(new ResourceNameSnapshot(), _reservations, new([(ExemptStore, [ExemptNamespace])]), Microsoft.Extensions.Logging.Abstractions.NullLogger<ResourceNameGate>.Instance);
    }

    void Because()
    {
        _store = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName(ExemptStore)));
        _namespace = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName(ExemptStore), new EventStoreNamespaceName(ExemptNamespace)));
        _otherNamespace = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName(ExemptStore), new EventStoreNamespaceName("a#b")));
        _created = Catch.Exception(() => _gate.EnsureCanCreate(new(new EventStoreName(ExemptStore), new EventStoreNamespaceName(ExemptNamespace), [])).GetAwaiter().GetResult());
        _unused = Catch.Exception(() => _ = new ResourceNameExemptions([("orders", [])]));
    }

    [Fact] void should_allow_the_exempt_store() => _store.ShouldBeNull();
    [Fact] void should_allow_the_exempt_namespace() => _namespace.ShouldBeNull();
    [Fact] void should_not_exempt_other_namespaces_of_the_store() => _otherNamespace.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_allow_creating_the_exempt_pair() => _created.ShouldBeNull();
    [Fact] void should_not_reserve_anything_for_the_exempt_pair() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
    [Fact] void should_refuse_an_exemption_that_is_not_needed() => _unused.ShouldBeOfExactType<UnusedResourceNameExemption>();
}
