// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.given;

public class two_identities_without_subject_registered : all_dependencies
{
    protected IdentityStorage store;
    protected IdentityId first_identity;
    protected IdentityId second_identity;
    protected MongoDBIdentity first_identity_from_database;
    protected MongoDBIdentity second_identity_from_database;

    void Establish()
    {
        first_identity = IdentityId.New();
        second_identity = IdentityId.New();

        first_identity_from_database = new MongoDBIdentity
        {
            Id = first_identity,
            Subject = string.Empty,
            Name = "First name",
            UserName = "First user name"
        };

        _identitiesFromDatabase.Add(first_identity_from_database);

        second_identity_from_database = new MongoDBIdentity
        {
            Id = second_identity,
            Subject = string.Empty,
            Name = "Second name",
            UserName = "Second user name"
        };
        _identitiesFromDatabase.Add(second_identity_from_database);

        store = new(_database, Mock.Of<ILogger<IdentityStorage>>());
    }
}
