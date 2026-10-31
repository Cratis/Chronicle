// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.when_normalizing;

public class nothing_configured : Specification
{
    string _result;

    void Because() => _result = WorkbenchBasePath.Normalize(null);

    [Fact] void should_be_the_root() => _result.ShouldEqual("");
}
