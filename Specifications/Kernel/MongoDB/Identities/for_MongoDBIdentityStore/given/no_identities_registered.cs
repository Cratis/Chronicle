// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.MongoDB.Identities.for_MongoDBIdentityStore.given;

public class no_identities_registered : all_dependencies
{
    protected MongoDBIdentityStore store;

    void Establish()
    {
        store = new(database.Object, Mock.Of<ILogger<MongoDBIdentityStore>>());
    }
}
