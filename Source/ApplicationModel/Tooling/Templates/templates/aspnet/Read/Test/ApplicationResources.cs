// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Infrastructure;

namespace Read.Applications
{
    public record ApplicationResources(IpAddress IpAddress, MongoDBResource MongoDB);
}
