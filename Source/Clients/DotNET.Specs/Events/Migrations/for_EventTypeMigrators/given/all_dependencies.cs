// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.given;

public class all_dependencies : Specification
{
    protected IClientArtifactsProvider _clientArtifactsProvider;
    protected IServiceProvider _serviceProvider;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _clientArtifactsProvider.EventTypeMigrators.Returns([]);

        _serviceProvider = Substitute.For<IServiceProvider>();
    }
}
