// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration.for_Adapters.given;

public class no_adapters : all_dependencies
{
    protected Adapters adapters;

    void Establish() => adapters = new(
            client_artifacts.Object,
            service_provider.Object,
            projection_factory.Object,
            mapper_factory.Object);
}
