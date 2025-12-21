// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Grains.Security;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles security events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SecurityReactor"/> class.
/// </remarks>
/// <param name="userStorage">The <see cref="IUserStorage"/> for managing users.</param>
/// <param name="clientCredentialsStorage">The <see cref="IClientCredentialsStorage"/> for managing client credentials.</param>
public class SecurityReactor(IUserStorage userStorage, IClientCredentialsStorage clientCredentialsStorage) : Reactor
{
    /// <summary>
    /// Handles the addition of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(UserAdded @event, EventContext eventContext)
    {
        var user = new Chronicle.Storage.Security.ChronicleUser(
            @event.UserId,
            @event.Username,
            @event.Email,
            @event.PasswordHash,
            Guid.NewGuid().ToString(),
            true,
            DateTimeOffset.UtcNow,
            null);

        await userStorage.Create(user);
    }

    /// <summary>
    /// Handles the removal of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(UserRemoved @event, EventContext eventContext)
    {
        await userStorage.Delete(@event.UserId);
    }

    /// <summary>
    /// Handles the password change of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task PasswordChanged(UserPasswordChanged @event, EventContext eventContext)
    {
        var user = await userStorage.GetById(@event.UserId);
        if (user is not null)
        {
            var updatedUser = user with
            {
                PasswordHash = @event.PasswordHash,
                SecurityStamp = Guid.NewGuid().ToString(),
                LastModifiedAt = DateTimeOffset.UtcNow
            };
            await userStorage.Update(updatedUser);
        }
    }

    /// <summary>
    /// Handles the addition of client credentials.
    /// </summary>
    /// <param name="event">The event containing the client credentials information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(ClientCredentialsAdded @event, EventContext eventContext)
    {
        var client = new Chronicle.Storage.Security.ChronicleClient(
            @event.Id,
            @event.ClientId,
            @event.ClientSecret,
            true,
            DateTimeOffset.UtcNow,
            null);

        await clientCredentialsStorage.Create(client);
    }

    /// <summary>
    /// Handles the removal of client credentials.
    /// </summary>
    /// <param name="event">The event containing the client credentials information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(ClientCredentialsRemoved @event, EventContext eventContext)
    {
        await clientCredentialsStorage.Delete(@event.Id);
    }

    /// <summary>
    /// Handles the secret change of client credentials.
    /// </summary>
    /// <param name="event">The event containing the client credentials information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task SecretChanged(ClientCredentialsSecretChanged @event, EventContext eventContext)
    {
        var client = await clientCredentialsStorage.GetById(@event.Id);
        if (client is not null)
        {
            var updatedClient = client with
            {
                ClientSecret = @event.ClientSecret,
                LastModifiedAt = DateTimeOffset.UtcNow
            };
            await clientCredentialsStorage.Update(updatedClient);
        }
    }
}
