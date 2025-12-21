// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Grains.Security;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles client credentials events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientCredentialsReactor"/> class.
/// </remarks>
/// <param name="applicationStorage">The <see cref="IApplicationStorage"/> for managing client credentials.</param>
public class ClientCredentialsReactor(IApplicationStorage applicationStorage) : Reactor
{
    /// <summary>
    /// Handles the addition of client credentials.
    /// </summary>
    /// <param name="event">The event containing the client credentials information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(ClientCredentialsAdded @event, EventContext eventContext)
    {
        var application = new Application
        {
            Id = @event.Id,
            ClientId = @event.ClientId,
            ClientSecret = @event.ClientSecret
        };

        await applicationStorage.Create(application);
    }

    /// <summary>
    /// Handles the removal of client credentials.
    /// </summary>
    /// <param name="event">The event containing the client credentials information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(ClientCredentialsRemoved @event, EventContext eventContext)
    {
        await applicationStorage.Delete(@event.Id);
    }

    /// <summary>
    /// Handles the secret change of client credentials.
    /// </summary>
    /// <param name="event">The event containing the client credentials information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task SecretChanged(ClientCredentialsSecretChanged @event, EventContext eventContext)
    {
        var application = await applicationStorage.GetById(@event.Id);
        if (application is not null)
        {
            var updatedApplication = application with
            {
                ClientSecret = @event.ClientSecret
            };
            await applicationStorage.Update(updatedApplication);
        }
    }
}
