// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.given;

public class no_identities_registered : all_dependencies
{
    protected IdentityStorage store;

    void Establish()
    {
        store = new(_database, Mock.Of<ILogger<IdentityStorage>>());
    }
}
