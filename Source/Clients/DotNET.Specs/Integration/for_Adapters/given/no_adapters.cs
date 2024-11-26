// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Adapters.given;

public class no_adapters : all_dependencies
{
    protected Adapters _adapters;

    void Establish() => _adapters = new(
            _clientArtifacts,
            _serviceProvider,
            _projectionFactory,
            _mapperFactory);
}
