// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
using Aksio.Queries.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Cratis.Compliance.Read.Microservices
{
    /// <summary>
    /// Represents the read side API for microservices.
    /// </summary>
    [Route("/api/compliance/microservices")]
    public class Microservices : Controller
    {
        readonly IMongoCollection<Microservice> _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="Microservices"/> class.
        /// </summary>
        /// <param name="collection">Mongo collection.</param>
        public Microservices(IMongoCollection<Microservice> collection) => _collection = collection;

        /// <summary>
        /// Get all configured microservices.
        /// </summary>
        /// <returns>Client observable of a collection of <see cref="Microservice"/>.</returns>
        [HttpGet]
        public Task<ClientObservable<IEnumerable<Microservice>>> AllMicroservices() => _collection.Observe();
    }
}
