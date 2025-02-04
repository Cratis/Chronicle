// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
public class CustomSerializersRegistrationService(IServiceProvider serviceProvider, ITypes types) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var type in types.FindMultiple<IBsonSerializer>()
                     .Where(type => type.Assembly.FullName!.Contains("Chronicle") && !type.IsGenericType))
        {
            var serializer = (IBsonSerializer)ActivatorUtilities.CreateInstance(serviceProvider, type);
            BsonSerializer.TryRegisterSerializer(serializer.ValueType, serializer);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
