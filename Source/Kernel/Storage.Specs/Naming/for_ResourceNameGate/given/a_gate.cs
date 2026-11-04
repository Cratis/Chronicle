// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate.given;

public class a_gate : Specification
{
    protected static readonly string[] HostileEventStores =
        ["a+es+b", "a#b", "a~0123456789abcdef", "a$b", "a\0b", "a\tb", "a\u0085b", "es", "ES", "admin", "Local", "config", "[NotSet]", " "];

    protected static readonly string[] HostileNamespaces =
        ["a+es+b", "a#b", "a~0123456789abcdef", "a$b", "a\0b", "a\tb", "a\u0085b", "es", "Es", " "];

    protected static readonly string[] ValidNames =
        ["orders", "Ørsted-prod_1", "a.b", "a b", "a:b", "a@b", "System", "Default", "e\u0301"];

    protected ResourceNameSnapshot _snapshot;
    protected IResourceNameReservations _reservations;
    protected ResourceNameGate _gate;

    protected virtual ResourceNameExemptions Exemptions => ResourceNameExemptions.None;

    void Establish()
    {
        _snapshot = new();
        _reservations = Substitute.For<IResourceNameReservations>();
        _reservations.Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(true);
        _gate = new(_snapshot, _reservations, Exemptions, NullLogger<ResourceNameGate>.Instance);
    }

    protected static ResourceCreation Store(string name, IEnumerable<string>? registered = null) =>
        new(new EventStoreName(name), null, registered ?? []);

    protected static ResourceCreation Namespace(
        string store,
        string name,
        IEnumerable<string>? registered = null,
        IEnumerable<string>? eventStores = null,
        Func<string, Task<IEnumerable<string>>>? namespacesOf = null) =>
        new(new EventStoreName(store), new EventStoreNamespaceName(name), registered ?? [], eventStores, namespacesOf);
}
