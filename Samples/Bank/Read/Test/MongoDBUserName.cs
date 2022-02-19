// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.MongoDB
{
    public record MongoDBUserName(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator MongoDBUserName(string value) => new(value);
    }
}
