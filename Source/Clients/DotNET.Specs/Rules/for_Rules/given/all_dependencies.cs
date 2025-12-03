// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Rules.for_Rules.given;

public class all_dependencies : Specification
{
    protected IClientArtifactsProvider _clientArtifacts;
    protected IProjections _projections;
    protected INamingPolicy _namingPolicy;

    void Establish()
    {
        _namingPolicy = new DefaultNamingPolicy();
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _projections = Substitute.For<IProjections>();
    }
}
