// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using ProtoBuf.Grpc.Configuration;


namespace Cratis.Server
{
    public class ServiceBinderWithServiceResolutionFromServiceCollection : ServiceBinder
    {
        readonly IServiceCollection _services;

        public ServiceBinderWithServiceResolutionFromServiceCollection(IServiceCollection services)
        {
            _services = services;
        }

        public override IList<object> GetMetadata(MethodInfo method, Type contractType, Type serviceType)
        {
            var resolvedServiceType = serviceType;
            if (serviceType.IsInterface)
                resolvedServiceType = _services.SingleOrDefault(x => x.ServiceType == serviceType)?.ImplementationType ?? serviceType;

            return base.GetMetadata(method, contractType, resolvedServiceType);
        }
    }
}
