// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given;

public class a_pii_manager : Specification
{
    protected static readonly EventStoreName _eventStore = "the-store";
    protected static readonly EventStoreNamespaceName _namespace = "the-namespace";

    protected ICompliance _compliance;
    protected PIIManager _manager;

    void Establish()
    {
        _compliance = Substitute.For<ICompliance>();

        var services = Substitute.For<IServices>();
        services.Compliance.Returns(_compliance);

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);

        _manager = new PIIManager(_eventStore, _namespace, connection);
    }
}
