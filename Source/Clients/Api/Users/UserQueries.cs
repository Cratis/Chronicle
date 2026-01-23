// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using System.Reactive.Subjects;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the API for user queries.
/// </summary>
[Route("/api/users")]
public class UserQueries : ControllerBase
{
    readonly IChronicleConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserQueries"/> class.
    /// </summary>
    /// <param name="connection">The <see cref="IChronicleConnection"/>.</param>
    public UserQueries(IChronicleConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Get all users.
    /// </summary>
    /// <returns>Collection of users.</returns>
    [HttpGet]
    public async Task<IEnumerable<User>> GetAllUsers()
    {
        var eventStore = await _connection.GetEventStore();
        return await eventStore.ReadModels.GetAll<User>();
    }

    /// <summary>
    /// Get a user by identifier.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user if found.</returns>
    [HttpGet("{userId}")]
    public async Task<ActionResult<User>> GetUser([FromRoute] Guid userId)
    {
        var eventStore = await _connection.GetEventStore();
        var user = await eventStore.ReadModels.GetById<User>(userId.ToString());
        
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Check if the admin user needs configuration.
    /// </summary>
    /// <returns>True if admin needs configuration, false otherwise.</returns>
    [HttpGet("admin/needs-configuration")]
    public async Task<bool> AdminNeedsConfiguration()
    {
        var eventStore = await _connection.GetEventStore();
        var admin = await eventStore.ReadModels.GetById<User>(UserId.Admin.ToString());
        
        return admin is null || string.IsNullOrWhiteSpace(admin.PasswordHash);
    }

    /// <summary>
    /// Observe all users.
    /// </summary>
    /// <returns>Observable collection of users.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<User>> ObserveAllUsers() =>
        _connection.InvokeAndWrapWithTransformSubject(
            async token =>
            {
                var eventStore = await _connection.GetEventStore();
                return eventStore.ReadModels.Observe<User>(token);
            },
            users => users);
}
