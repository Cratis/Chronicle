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
        var marshallerCacheProperty = BinderConfiguration.GetType().GetProperty("MarshallerCache", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var marshallerCache = marshallerCacheProperty.GetValue(BinderConfiguration)!;
        var getMarshallerGenericMethod = marshallerCache.GetType().GetMethod("GetMarshaller", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        var serviceType = typeof(TService);
        var implementationTypeName = $"{serviceType.Namespace}.{serviceType.Name.Substring(1)}";
        var implementationType = serviceType.Assembly.GetType(implementationTypeName) as TypeInfo;
        if (implementationType is not null)
        {
            var instance = (TService)Activator.CreateInstance(implementationType, channel)!;
            var marshallerFields = implementationType.DeclaredFields;

#pragma warning disable RCS1201 // Use method chaining
            marshallerFields = marshallerFields
                            .Where(_ => _.FieldType.IsGenericType)
                            .ToArray();
#pragma warning restore RCS1201 // Use method chaining

            marshallerFields = marshallerFields
                .Where(_ => _.FieldType.IsGenericType && _.FieldType.GetGenericTypeDefinition() == typeof(Marshaller<>))
                .ToArray();

            foreach (var field in marshallerFields)
            {
                var getMarshallerMethod = getMarshallerGenericMethod.MakeGenericMethod(field.FieldType.GetGenericArguments()[0]);
                var marshaller = getMarshallerMethod.Invoke(marshallerCache, [])!;
                field.SetValue(instance, marshaller);
            }

            var initMethod = implementationType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
            initMethod?.Invoke(null, []);
            return instance;
        }

        return Default.CreateClient<TService>(channel);
    }
}
