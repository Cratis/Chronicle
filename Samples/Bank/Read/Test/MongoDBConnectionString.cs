// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.MongoDB
{
    public record MongoDBConnectionString(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator MongoDBConnectionString(string value) => new(value);
    }
}
