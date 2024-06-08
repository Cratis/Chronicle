// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Compliance.GDPR;

/// <summary>
/// Represents the API for working with people.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="People"/> class.
/// </remarks>
[Route("/api/compliance/gdpr/people")]
public class People() : ControllerBase
{
    /// <summary>
    /// Get all people.
    /// </summary>
    /// <returns>Client observable of a collection of <see cref="Person">people</see>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<Person>>> AllPeople() => throw new NotImplementedException();

    /// <summary>
    /// Search for people by an arbitrary string.
    /// </summary>
    /// <param name="query">String to search for.</param>
    /// <returns>Collection of matching <see cref="Person">people</see>.</returns>
    [HttpGet("search")]
    public async Task<IEnumerable<Person>> SearchForPeople([FromQuery] string query)
    {
        throw new NotImplementedException();
    }
}
