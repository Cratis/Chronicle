// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.MongoDB;

namespace Events.Applications
{
    [EventType("bf5ea49f-44ab-4e43-90ff-054355e65a67")]
    public record MongoDBUserChanged(MongoDBUserName User, MongoDBPassword Password);
}
