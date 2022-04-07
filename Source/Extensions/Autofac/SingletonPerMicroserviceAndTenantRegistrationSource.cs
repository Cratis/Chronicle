// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Types;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace Aksio.Cratis.Extensions.Autofac;

/// <summary>
/// Represents a <see cref="IRegistrationSource"/> to handle correct lifecycle for implementations marked with <see cref="SingletonPerMicroserviceAndTenantAttribute"/>.
/// </summary>
public class SingletonPerMicroserviceAndTenantRegistrationSource : IRegistrationSource
{
    record ImplementationTypeAndMicroserviceAndTenant(Type ImplementationType, MicroserviceId MicroserviceId, TenantId TenantId);
    readonly ConcurrentDictionary<ImplementationTypeAndMicroserviceAndTenant, object> _instancesPerMicroservice = new();

    readonly ContractToImplementorsMap _implementorsMap = new();

    /// <inheritdoc/>
    public bool IsAdapterForIndividualComponents => false;

    /// <summary>
    /// Initializes a new instance of <see cref="SingletonPerMicroserviceAndTenantRegistrationSource"/>.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> to discover implementors marked with <see cref="SingletonPerMicroserviceAndTenantAttribute"/>.</param>
    public SingletonPerMicroserviceAndTenantRegistrationSource(ITypes types)
    {
        _implementorsMap.Feed(types.All.Where(_ => _.HasAttribute<SingletonPerMicroserviceAndTenantAttribute>()));
    }

    /// <inheritdoc/>
    public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (!(service is IServiceWithType) || service is not IServiceWithType serviceWithType)
        {
            return Enumerable.Empty<IComponentRegistration>();
        }

        var implementors = _implementorsMap.GetImplementorsFor(serviceWithType.ServiceType);
        if (!implementors.Any()) return Enumerable.Empty<IComponentRegistration>();

        var implementationType = implementors.First();
        var registration = RegistrationBuilder
                            .ForDelegate(implementors.First(), (_, __) => Resolve(implementationType))
                            .As(serviceWithType.ServiceType).CreateRegistration();

        return new[] { registration };
    }

    object Resolve(Type implementationType)
    {
        var executionContext = ExecutionContextManager.GetCurrent();
        var key = new ImplementationTypeAndMicroserviceAndTenant(implementationType, executionContext.MicroserviceId, executionContext.TenantId);
        if (_instancesPerMicroservice.ContainsKey(key))
        {
            return _instancesPerMicroservice[key];
        }
        return _instancesPerMicroservice[key] = ContainerBuilderExtensions.Container!.Resolve(implementationType);
    }
}
