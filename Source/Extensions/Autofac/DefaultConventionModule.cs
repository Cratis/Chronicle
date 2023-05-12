// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Reflection;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Extensions.Autofac;

/// <summary>
/// Represents a <see cref="Module">autofac module</see> for default convention (IFoo -> Foo).
/// </summary>
public class DefaultConventionModule : Module
{
    readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConventionModule"/> class.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to prevent registering already registered services.</param>
    public DefaultConventionModule(IServiceCollection services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        bool MatchesConvention(Type type)
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                var conventionInterface = interfaces.SingleOrDefault(i => i.Namespace == type.Namespace && i.Name == $"I{type.Name}");
                if (conventionInterface != default)
                {
                    bool HasInterface(Type type) => type.HasInterface(conventionInterface);
                    return ContainerBuilderExtensions.Types!.All.Count(HasInterface) == 1;
                }
            }
            return false;
        }

        foreach (var conventionBasedType in ContainerBuilderExtensions.Types!.All.Where(MatchesConvention))
        {
            var interfaceToBind = conventionBasedType.GetInterfaces().Single(_ => _.Name == $"I{conventionBasedType.Name}");
            if (_services.Any(_ => _.ServiceType == interfaceToBind))
            {
                continue;
            }

            if (interfaceToBind.IsGenericType)
            {
                var result = builder.RegisterGeneric(conventionBasedType).As(interfaceToBind);
                if (conventionBasedType.HasAttribute<SingletonAttribute>()) result.SingleInstance();
                if (conventionBasedType.HasAttribute<SingletonPerTenantAttribute>()) result.InstancePerTenant();
                if (conventionBasedType.HasAttribute<SingletonPerMicroserviceAttribute>()) result.InstancePerMicroservice();
                if (conventionBasedType.HasAttribute<SingletonPerMicroserviceAndTenantAttribute>()) result.InstancePerMicroserviceAndTenant();
            }
            else
            {
                var result = builder.RegisterType(conventionBasedType).As(interfaceToBind);
                if (conventionBasedType.HasAttribute<SingletonAttribute>()) result.SingleInstance();
                if (conventionBasedType.HasAttribute<SingletonPerTenantAttribute>()) result.InstancePerTenant();
                if (conventionBasedType.HasAttribute<SingletonPerMicroserviceAttribute>()) result.InstancePerMicroservice();
                if (conventionBasedType.HasAttribute<SingletonPerMicroserviceAndTenantAttribute>()) result.InstancePerMicroserviceAndTenant();
            }
        }
    }
}
