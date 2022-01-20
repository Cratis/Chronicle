// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Integration.for_Adapters.given
{
    public class all_dependencies : Specification
    {
        protected Mock<ITypes> types;
        protected Mock<IServiceProvider> service_provider;
        protected Mock<IAdapterProjectionFactory> projection_factory;
        protected Mock<IAdapterMapperFactory> mapper_factory;

        void Establish()
        {
            types = new();
            service_provider = new();
            projection_factory = new();
            mapper_factory = new();
        }
    }
}
