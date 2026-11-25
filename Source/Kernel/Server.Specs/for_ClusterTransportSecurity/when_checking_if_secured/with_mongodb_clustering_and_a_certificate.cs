// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterTransportSecurity.when_checking_if_secured;

public class with_mongodb_clustering_and_a_certificate : given.a_certificate
{
    bool _result;

    void Because() => _result = ClusterTransportSecurity.IsSecured(new Clustering { Type = ClusteringType.MongoDB }, _certificate);

    [Fact] void should_be_secured() => _result.ShouldBeTrue();
}
