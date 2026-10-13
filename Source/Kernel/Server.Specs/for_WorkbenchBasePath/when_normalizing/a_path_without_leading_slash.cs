// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.when_normalizing;

public class a_path_without_leading_slash : Specification
{
    string _result;

    void Because() => _result = WorkbenchBasePath.Normalize("my-instance/chronicle");

    [Fact] void should_be_the_path_with_a_leading_slash() => _result.ShouldEqual("/my-instance/chronicle");
}
