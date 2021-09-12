// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Autofac.Builder;
using Autofac.Core;
using Cratis.Execution;

namespace Cratis.Configuration
{
    /// <summary>
    /// Represents a <see cref="IRegistrationSource"/> for automatically resolving <see cref="EventStoreConfigurationAs{T}"/> dependencies.
    /// </summary>
    public class EventStoreConfigurationAsRegistrationSource : IRegistrationSource
    {
        readonly MethodInfo _provideMethod = typeof(EventStoreConfigurationAsRegistrationSource)
                                                .GetMethod(nameof(EventStoreConfigurationAsRegistrationSource.Provide), BindingFlags.Instance | BindingFlags.NonPublic)!;
        readonly IStorageConfigurationManager _storageConfigurationManager;

        /// <summary>
        /// Initializes a new instance of <see cref="EventStoreConfigurationAsRegistrationSource"/>.
        /// </summary>
        /// <param name="storageConfigurationManager"><see cref="IStorageConfigurationManager"/> to work with config.</param>
        public EventStoreConfigurationAsRegistrationSource(IStorageConfigurationManager storageConfigurationManager)
        {
            _storageConfigurationManager = storageConfigurationManager;
        }

        /// <inheritdoc/>
        public bool IsAdapterForIndividualComponents => false;

        /// <inheritdoc/>
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (!(service is IServiceWithType) ||
                service is not IServiceWithType serviceWithType ||
                !serviceWithType.ServiceType.IsGenericType ||
                serviceWithType.ServiceType != typeof(EventStoreConfigurationAs<>).MakeGenericType(serviceWithType.ServiceType.GetGenericArguments()[0]))
            {
                return Enumerable.Empty<IComponentRegistration>();
            }

            var method = _provideMethod.MakeGenericMethod(serviceWithType.ServiceType.GetGenericArguments()[0]);
            var registration = RegistrationBuilder.ForDelegate(serviceWithType.ServiceType,
                (_, __) => method.CreateDelegate(serviceWithType.ServiceType, this))
                .As(serviceWithType.ServiceType)
                .CreateRegistration();

            return new[] { registration };
        }

        T Provide<T>()
        {
            return (T)_storageConfigurationManager.GetForEventStore(typeof(T), ExecutionContextManager.GetCurrent().TenantId);
        }
    }
}
