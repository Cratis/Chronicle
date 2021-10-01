// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Cratis.Execution;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Extensions.Autofac
{
    /// <summary>
    /// Represents a <see cref="IRegistrationSource"/> to handle correct lifecycle for implementations marked with <see cref="SingletonPerTenantAttribute"/>.
    /// </summary>
    public class SingletonPerTenantRegistrationSource : IRegistrationSource
    {
        public record ImplementationTypeAndTenant(Type ImplementationType, TenantId TenantId);
        readonly ConcurrentDictionary<ImplementationTypeAndTenant, object> _instancesPerTenant = new();

        readonly ContractToImplementorsMap _implementorsMap = new();

        /// <inheritdoc/>
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>
        /// Initializes a new instance of <see cref="SingletonPerTenantRegistrationSource"/>.
        /// </summary>
        /// <param name="types"><see cref="ITypes"/> to discover implementors marked with <see cref="SingletonPerTenantAttribute"/>.</param>
        public SingletonPerTenantRegistrationSource(ITypes types)
        {
            _implementorsMap.Feed(types.All.Where(_ => _.HasAttribute<SingletonPerTenantAttribute>()));
        }

        /// <inheritdoc/>
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (!(service is IServiceWithType) || service is not IServiceWithType serviceWithType) return Enumerable.Empty<IComponentRegistration>();

            var implementors = _implementorsMap.GetImplementorsFor(serviceWithType.ServiceType);
            if (!implementors.Any()) return Enumerable.Empty<IComponentRegistration>();

            var implementationType = implementors.First();
            var registration = RegistrationBuilder
                                .ForDelegate(implementors.First(), (_,__) => Resolve(implementationType))
                                .As(serviceWithType.ServiceType).CreateRegistration();

            return new[] { registration };
        }

        object Resolve(Type implementationType)
        {
            var key = new ImplementationTypeAndTenant(implementationType, ExecutionContextManager.GetCurrent().TenantId);
            if (_instancesPerTenant.ContainsKey(key))
            {
                return _instancesPerTenant[key];
            }
            return _instancesPerTenant[key] = ContainerBuilderExtensions.Container!.Resolve(implementationType);
        }
    }
}
