// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.when_resolving;

/// <summary>
/// The shape every container image has: the Kernel binary is published without the frontend output, so
/// nothing is embedded, and the image ships the built UI as files in the web root instead.
/// </summary>
public class and_the_ui_is_only_in_the_web_root : Specification
{
    given.a_file_provider _webRoot;
    IFileProvider? _result;

    void Establish() => _webRoot = new(WorkbenchUI.EntryPoint);

    void Because() => _result = WorkbenchUI.Resolve(null, _webRoot);

    [Fact] void should_serve_from_the_web_root_provider() => _result.ShouldEqual(_webRoot);
}
