// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jDriverFactory;

public class when_creating : Specification
{
    Neo4jDriverFactory _factory;
    IDriver _first;
    IDriver _same;
    IDriver _otherServer;
    IDriver _otherIdentity;
    int _created;

    void Establish() => _factory = new((_, _) =>
    {
        _created++;
        return Substitute.For<IDriver>();
    });

    void Because()
    {
        _first = _factory.Create(new("bolt://one:7687"), new ConfiguredNeo4jAuthentication("user", "secret"));
        _same = _factory.Create(new("bolt://one:7687"), new ConfiguredNeo4jAuthentication("user", "secret"));
        _otherServer = _factory.Create(new("bolt://two:7687"), new ConfiguredNeo4jAuthentication("user", "secret"));
        _otherIdentity = _factory.Create(new("bolt://one:7687"), new ConfiguredNeo4jAuthentication("other", "secret"));
    }

    [Fact] void should_share_the_driver_for_the_same_server_and_identity() => _same.ShouldEqual(_first);
    [Fact] void should_create_another_driver_for_another_server() => _otherServer.ShouldNotEqual(_first);
    [Fact] void should_create_another_driver_for_another_identity() => _otherIdentity.ShouldNotEqual(_first);
    [Fact] void should_create_three_drivers() => _created.ShouldEqual(3);
}
