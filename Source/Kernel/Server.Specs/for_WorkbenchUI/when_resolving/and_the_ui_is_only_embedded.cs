// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.when_resolving;

public class and_the_ui_is_only_embedded : Specification
{
    given.a_file_provider _embedded;
    IFileProvider? _result;

    void Establish() => _embedded = new(WorkbenchUI.EntryPoint);

    void Because() => _result = WorkbenchUI.Resolve(_embedded, given.a_file_provider.Empty());

    [Fact] void should_serve_from_the_embedded_provider() => _result.ShouldEqual(_embedded);
}
