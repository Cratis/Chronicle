// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.when_applying;

public class to_the_root : Specification
{
    given.an_index_page _inner;
    IFileProvider _result;

    void Establish() => _inner = new();

    void Because() => _result = WorkbenchBasePath.Apply(_inner, string.Empty);

    [Fact] void should_serve_from_the_provider_it_was_given() => _result.ShouldEqual(_inner);
}
