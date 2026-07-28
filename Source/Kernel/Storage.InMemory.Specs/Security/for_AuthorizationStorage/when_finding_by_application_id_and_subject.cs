// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_AuthorizationStorage;

public class when_finding_by_application_id_and_subject : given.an_authorization_storage
{
    static readonly ApplicationId _application = new(Guid.NewGuid());
    static readonly ApplicationId _otherApplication = new(Guid.NewGuid());
    IEnumerable<Authorization> _result;
    Authorization _match;

    async Task Establish()
    {
        _match = Authorization(_application, "user");
        await _storage.Create(_match);
        await _storage.Create(Authorization(_otherApplication, "user"));
        await _storage.Create(Authorization(_application, "other"));
    }

    async Task Because() => _result = await _storage.FindByApplicationIdAndSubject(_application, "user");

    [Fact] void should_return_only_the_matching_authorization() => _result.Select(_ => _.Id).ShouldContainOnly([_match.Id]);
}
