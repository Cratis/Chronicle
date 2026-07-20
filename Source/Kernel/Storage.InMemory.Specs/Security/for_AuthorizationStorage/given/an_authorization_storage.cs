// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_AuthorizationStorage.given;

public class an_authorization_storage : Specification
{
    protected AuthorizationStorage _storage;

    void Establish() => _storage = new();

    protected static Authorization Authorization(ApplicationId applicationId, Subject subject, DateTimeOffset? creationDate = null) =>
        new(new AuthorizationId(Guid.NewGuid()), applicationId, subject, null, "valid", [], creationDate, []);
}
