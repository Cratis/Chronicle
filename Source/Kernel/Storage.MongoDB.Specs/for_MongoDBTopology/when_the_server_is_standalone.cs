// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology.given;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology;

public class when_the_server_is_standalone : a_topology
{
    bool _first;
    bool _second;

    void Because()
    {
        _first = _topology.IsStandalone;
        _second = _topology.IsStandalone;
    }

    [Fact] void should_report_standalone() => _first.ShouldBeTrue();
    [Fact] void should_keep_reporting_standalone() => _second.ShouldBeTrue();
    [Fact] void should_ask_the_server_once() => _probes.ShouldEqual(1);
}
