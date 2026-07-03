// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ExternalServices;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents an implementation of <see cref="IExternalServices"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ExternalServices(IEventStore eventStore, ILogger<ExternalServices> logger) : IExternalServices
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task Register(string name, Action<IExternalServiceBuilder> configure)
    {
        var builder = new ExternalServiceBuilder();
        configure(builder);
        var definition = builder.Build(name, name);
        logger.RegisterExternalService(name);

        var request = new AddExternalServices
        {
            EventStore = eventStore.Name,
            ExternalServices = [definition]
        };

        await _servicesAccessor.Services.ExternalServices.Add(request);
    }
}
