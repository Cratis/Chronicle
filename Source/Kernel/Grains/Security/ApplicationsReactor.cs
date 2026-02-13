// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Grains.Security;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles application events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationsReactor"/> class.
/// </remarks>
/// <param name="applicationStorage">The <see cref="IApplicationStorage"/> for managing applications.</param>
public class ApplicationsReactor(IApplicationStorage applicationStorage) : Reactor
{
    /// <summary>
    /// Handles the addition of an application.
    /// </summary>
    /// <param name="event">The event containing the an application information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(ApplicationAdded @event, EventContext eventContext)
    {
        var application = new Application
        {
            Id = eventContext.EventSourceId,
            ClientId = @event.ClientId,
            ClientSecret = @event.ClientSecret,
            Type = "confidential",
            ConsentType = "implicit",
            Permissions =
            [
                "ept:token",
                "gt:client_credentials",
                "gt:password",
                "gt:refresh_token"
            ]
        };

        await applicationStorage.Create(application);
    }

    /// <summary>
    /// Handles the removal of an application.
    /// </summary>
    /// <param name="event">The event containing the an application information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(ApplicationRemoved @event, EventContext eventContext)
    {
        await applicationStorage.Delete(eventContext.EventSourceId);
    }

    /// <summary>
    /// Handles the secret change of an application.
    /// </summary>
    /// <param name="event">The event containing the an application information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task SecretChanged(ApplicationSecretChanged @event, EventContext eventContext)
    {
        var application = await applicationStorage.GetById(eventContext.EventSourceId);
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
