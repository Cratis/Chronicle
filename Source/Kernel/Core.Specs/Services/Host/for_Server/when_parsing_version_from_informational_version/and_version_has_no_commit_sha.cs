// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Host.for_Server.when_parsing_version_from_informational_version;

public class and_version_has_no_commit_sha : Specification
{
    string _result;

    void Because() => _result = Server.ParseVersionFromInformationalVersion("15.9.0");

    [Fact] void should_return_the_full_version_string() => _result.ShouldEqual("15.9.0");
}
