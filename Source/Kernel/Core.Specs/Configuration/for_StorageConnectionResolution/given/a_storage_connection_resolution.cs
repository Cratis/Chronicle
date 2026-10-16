// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration.for_StorageConnectionResolution.given;

public class a_storage_connection_resolution : Specification
{
    protected ConnectionResolutionOptions _options;
    protected ICachingConnectionResolver _resolver;
    protected IConnectionResolverFactory _factory;
    protected StorageConnectionResolution _resolution;
    protected ConnectionRequest? _request;

    void Establish()
    {
        _options = new();
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(Arg.Do<ConnectionRequest>(_ => _request = _), Arg.Any<CancellationToken>())
            .Returns(new ResolvedConnection("Server=placed", "placed", "placed_schema", new(AccessToken: "token")));
        _factory = Substitute.For<IConnectionResolverFactory>();
        _factory.Create(Arg.Any<ConnectionResolutionOptions>()).Returns(_resolver);
        _resolution = new(WellKnownConnectionStorage.PostgreSql, _options, _factory);
    }
}
