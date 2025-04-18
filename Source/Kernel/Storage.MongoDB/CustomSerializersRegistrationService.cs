// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Reflection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an <see cref="IHostedService"/> that registers our custom bson serializers.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="types">The <see cref="ITypes"/>.</param>
public class CustomSerializersRegistrationService(IServiceProvider serviceProvider, ITypes types) : IStartupTask
{
    /// <inheritdoc/>
    public Task Execute(CancellationToken cancellationToken)
    {
        foreach (var type in types.FindMultiple<IBsonSerializationProvider>().Where(IsEligibleForAutoRegistration))
        {
            var provider = (IBsonSerializationProvider)ActivatorUtilities.CreateInstance(serviceProvider, type);
            BsonSerializer.RegisterSerializationProvider(provider);
        }
        foreach (var type in types.FindMultiple<IBsonSerializer>().Where(IsEligibleForAutoRegistration))
        {
            var serializer = (IBsonSerializer)ActivatorUtilities.CreateInstance(serviceProvider, type);
            BsonSerializer.TryRegisterSerializer(serializer.ValueType, serializer);
        }
        return Task.CompletedTask;
    }

    static bool IsEligibleForAutoRegistration(Type type) => type.Assembly.FullName!.Contains("Cratis.Chronicle") &&
                                                            !type.IsGenericType &&
                                                            !type.HasAttribute<BsonSerializerDisableAutoRegistrationAttribute>();
}
