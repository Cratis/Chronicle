// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Host.for_Server.when_parsing_commit_sha_from_informational_version;

public class and_version_has_commit_sha : Specification
{
    string _result;

    void Because() => _result = Server.ParseCommitShaFromInformationalVersion("15.9.0+abc123def456");

    [Fact] void should_return_the_commit_sha() => _result.ShouldEqual("abc123def456");
}
