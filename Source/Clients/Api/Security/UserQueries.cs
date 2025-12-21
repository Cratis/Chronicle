// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the API for queries related to users.
/// </summary>
[Route("/api/security/users")]
public class UserQueries : ControllerBase
{
    readonly IUsers _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserQueries"/> class.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    internal UserQueries(IUsers users)
    {
        _users = users;
    }

    /// <summary>
    /// Get all users.
    /// </summary>
    /// <returns>A collection of users.</returns>
    [HttpGet]
    public async Task<IEnumerable<User>> GetUsers() =>
        (await _users.GetAll()).ToApi();

    /// <summary>
    /// Observes all users.
    /// </summary>
    /// <returns>An observable for observing a collection of users.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<User>> AllUsers() =>
        _users.InvokeAndWrapWithTransformSubject(
            token => _users.ObserveAll(token),
            users => users.ToApi());
}
