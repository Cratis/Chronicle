// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationPolicies.given;

public class policies_on_a_substitute_kernel : Specification
{
    protected IFederation _wire;
    protected FederationPolicies _policies;

    void Establish()
    {
        _wire = Substitute.For<IFederation>();
        var services = Substitute.For<IServices>();
        services.Federation.Returns(_wire);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _policies = new(connection);
    }
}
