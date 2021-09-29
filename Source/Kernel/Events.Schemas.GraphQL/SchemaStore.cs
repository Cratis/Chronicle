// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Schemas.GraphQL
{
    [GraphRoot("events/schemas")]
    public class SchemaStore : GraphController
    {
        [Query]
        public Task<int> GetStuff()
        {
            return Task.FromResult(42);
        }
    }
}
