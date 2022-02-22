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
        var conventionBasedTypes = ContainerBuilderExtensions.Types!.All.Where(_ =>
        {
            var interfaces = _.GetInterfaces();
            if (interfaces.Length > 0)
            {
                var conventionInterface = interfaces.SingleOrDefault(i => i.Namespace == _.Namespace && i.Name == $"I{_.Name}");
                if (conventionInterface != default)
                {
                    return ContainerBuilderExtensions.Types!.All.Count(type => type.HasInterface(conventionInterface)) == 1;
                }
            }
            return false;
        });

        foreach (var conventionBasedType in conventionBasedTypes)
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
            }
            else
            {
                var result = builder.RegisterType(conventionBasedType).As(interfaceToBind);
                if (conventionBasedType.HasAttribute<SingletonAttribute>()) result.SingleInstance();
            }
        }
    }
}
