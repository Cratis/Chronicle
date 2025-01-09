// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Grpc.Core;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents a factory for creating Grpc Clients, preferring if there is an existing implementation in the same namespace or not.
/// </summary>
public class InProcessAwareGrpcClientProxiesClientFactory : ClientFactory
{
    /// <inheritdoc/>
    protected override BinderConfiguration BinderConfiguration => BinderConfiguration.Default;

    /// <inheritdoc/>
    public override TService CreateClient<TService>(CallInvoker channel)
    {
        var marshallerCache = BinderConfiguration.GetType().GetProperty("MarshallerCache", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var getMarshallerMethod = marshallerCache.GetType().GetMethod("GetMarshaller", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        var serviceType = typeof(TService);
        var implementationTypeName = $"{serviceType.Namespace}.{serviceType.Name.Substring(1)}";
        var implementationType = serviceType.Assembly.GetType(implementationTypeName);
        if (implementationType is not null)
        {
            var instance = (TService)Activator.CreateInstance(implementationType, channel)!;
            var marshallerFields = implementationType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(_ => _.Name.StartsWith("_m")).ToArray();
            var methods = serviceType.GetMethods();

            // Get return value and find a matching field (_m*)
            // Get input parameter and find a matching field (_m*)
            // If there is no return type or input parameter type, find a matching Marshaller<Empty> field
            // Get marshaller from cache for each field and initialize all fields
            var initMethod = implementationType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
            initMethod?.Invoke(null, []);
            return instance;
        }

        return Default.CreateClient<TService>(channel);
    }
}
