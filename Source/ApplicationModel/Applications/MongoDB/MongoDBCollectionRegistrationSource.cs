// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Hosting;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using MongoDB.Driver;

namespace Aksio.Cratis.Applications.MongoDB;

/// <summary>
/// Represents a <see cref="IRegistrationSource"/> for automatically hooking up dependencies to <see cref="IMongoCollection{T}"/> that are not already registered.
/// </summary>
public class MongoDBCollectionRegistrationSource : IRegistrationSource
{
    static readonly MethodInfo _getCollectionMethod = typeof(IMongoDatabase).GetMethod(nameof(IMongoDatabase.GetCollection), BindingFlags.Public | BindingFlags.Instance)!;

    /// <inheritdoc/>
    public bool IsAdapterForIndividualComponents => false;

    /// <inheritdoc/>
    public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (!(service is IServiceWithType) ||
            service is not IServiceWithType serviceWithType ||
            !serviceWithType.ServiceType.IsGenericType ||
            serviceWithType.ServiceType != typeof(IMongoCollection<>).MakeGenericType(serviceWithType.ServiceType.GetGenericArguments()[0]))
        {
            return Enumerable.Empty<IComponentRegistration>();
        }

        var registration = RegistrationBuilder.ForDelegate(
            serviceWithType.ServiceType,
            (context, _) =>
            {
                var database = context.Resolve<IMongoDatabase>();
                var readModelType = serviceWithType.ServiceType.GetGenericArguments()[0];
                var modelName = MongoDBReadModels.GetReadModelName(readModelType);
                var genericMethod = _getCollectionMethod.MakeGenericMethod(readModelType);
                return genericMethod.Invoke(database, new object[] { modelName, null! })!;
            })
            .As(serviceWithType.ServiceType)
            .CreateRegistration();

        return new[] { registration };
    }
}
