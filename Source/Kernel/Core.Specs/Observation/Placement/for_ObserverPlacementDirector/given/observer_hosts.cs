// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clustering;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector.given;

public class observer_hosts : Specification
{
    protected IClusterReleases _releases;
    protected IPlacementContext _context;
    protected ChronicleOptions _options;
    protected ObserverPlacementDirector _director;
    protected SiloAddress _local;
    protected SiloAddress _other;
    protected SiloAddress _result;

    void Establish()
    {
        _local = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _other = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        _context = Substitute.For<IPlacementContext>();
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_local, _other]);
        _releases = Substitute.For<IClusterReleases>();
        _options = new();
        _options.Clustering.Roles.Observers = true;
        var local = Substitute.For<ILocalSiloDetails>();
        local.SiloAddress.Returns(_local);
        _director = new(Options.Create(_options), local, _releases);
    }

    protected async Task Place() => _result = await _director.OnAddActivation(null!, default, _context);
}
