// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Cratis.Monads;
using Microsoft.Extensions.Options;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Cluster.given;

public class a_role_placement : Specification
{
    protected SiloAddress _local;
    protected SiloAddress _remote;
    protected SiloAddress _unknown;
    protected ISiloRoles _siloRoles;
    protected ChronicleOptions _options;
    protected RoleEligibility _eligibility;
    protected IPlacementContext _context;
    protected SiloAddress _selected;

    void Establish()
    {
        _local = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _remote = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        _unknown = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11113), 1);
        _siloRoles = Substitute.For<ISiloRoles>();
        _siloRoles.GetRoles(Arg.Any<SiloAddress>()).Returns(Option<ClusterRoles>.None());
        _options = new();
        var localDetails = Substitute.For<ILocalSiloDetails>();
        localDetails.SiloAddress.Returns(_local);
        _eligibility = new(_siloRoles, localDetails, Options.Create(_options));
        _context = Substitute.For<IPlacementContext>();
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_local, _remote, _unknown]);
    }
}
