// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Applications.Queries.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Cratis.Kernel.Read.Compliance.GDPR;

/// <summary>
/// Represents the API for working with people.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="People"/> class.
/// </remarks>
/// <param name="collection">Mongo collection.</param>
[Route("/api/compliance/gdpr/people")]
public class People(IMongoCollection<Person> collection) : ControllerBase
{
    /// <summary>
    /// Get all people.
    /// </summary>
    /// <returns>Client observable of a collection of <see cref="Person">people</see>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<Person>>> AllPeople() => collection.Observe();

    /// <summary>
    /// Search for people by an arbitrary string.
    /// </summary>
    /// <param name="query">String to search for.</param>
    /// <returns>Collection of matching <see cref="Person">people</see>.</returns>
    [HttpGet("search")]
    public async Task<IEnumerable<Person>> SearchForPeople([FromQuery] string query)
    {
        var filter = Builders<Person>.Filter.Text(query, new TextSearchOptions { CaseSensitive = false, DiacriticSensitive = false });
        var result = await collection.FindAsync(filter);
        return result.ToList();
    }
}
