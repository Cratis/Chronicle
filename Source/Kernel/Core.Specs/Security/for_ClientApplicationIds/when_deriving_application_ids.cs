// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Security.for_ClientApplicationIds;

public class when_deriving_application_ids : Specification
{
    ApplicationId _first;
    ApplicationId _second;
    ApplicationId _other;

    void Because()
    {
        _first = ClientApplicationIds.For("chronicle-dev-client");
        _second = ClientApplicationIds.For("chronicle-dev-client");
        _other = ClientApplicationIds.For("another-client");
    }

    [Fact] void should_be_stable_for_the_same_client_id() => _second.ShouldEqual(_first);
    [Fact] void should_differ_per_client_id() => _other.ShouldNotEqual(_first);
    [Fact] void should_be_the_rfc_version_5_uuid_in_the_namespace() => _first.Value.ShouldEqual(new Guid("9677f11c-039c-5fca-b22c-d8d56702ed2c"));
}
