// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Cratis.DependencyInversion;

namespace Cratis.Extensions.Autofac
{
    /// <summary>
    /// Represents a <see cref="IRegistrationSource"/> for automatically resolving <see cref="ProviderFor{T}"/> dependencies.
    /// </summary>
    public class ProviderForRegistrationSource : IRegistrationSource
    {
        readonly MethodInfo _provideMethod = typeof(ProviderForRegistrationSource)
                                                .GetMethod(nameof(ProviderForRegistrationSource.Provide), BindingFlags.Static | BindingFlags.NonPublic)!;

        /// <inheritdoc/>
        public bool IsAdapterForIndividualComponents => false;

        /// <inheritdoc/>
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (!(service is IServiceWithType) ||
                service is not IServiceWithType serviceWithType ||
                !serviceWithType.ServiceType.IsGenericType ||
                serviceWithType.ServiceType != typeof(ProviderFor<>).MakeGenericType(serviceWithType.ServiceType.GetGenericArguments()[0]))
            {
                return Enumerable.Empty<IComponentRegistration>();
            }

            var method = _provideMethod.MakeGenericMethod(serviceWithType.ServiceType.GetGenericArguments()[0]);
            var registration = RegistrationBuilder.ForDelegate(serviceWithType.ServiceType,
                (_, __) => method.CreateDelegate(serviceWithType.ServiceType))
                .As(serviceWithType.ServiceType)
                .CreateRegistration();

            return new[] { registration };
        }

        static T Provide<T>()
            where T : notnull
        {
            return ContainerBuilderExtensions.Container!.Resolve<T>();
        }
    }
}
