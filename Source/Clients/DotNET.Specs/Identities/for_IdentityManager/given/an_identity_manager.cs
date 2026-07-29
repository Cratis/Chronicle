// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Identities.for_IdentityManager.given;

public class an_identity_manager : Specification
{
    protected static readonly EventStoreName _eventStore = "the-store";
    protected static readonly EventStoreNamespaceName _namespace = "the-namespace";

    protected IIdentities _identities;
    protected IdentityManager _manager;

    void Establish()
    {
        _identities = Substitute.For<IIdentities>();

        var services = Substitute.For<IServices>();
        services.Identities.Returns(_identities);

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);

        _manager = new IdentityManager(_eventStore, _namespace, connection);
    }
}
