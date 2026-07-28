// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.when_resolving;

/// <summary>
/// An assembly can carry an embedded files manifest for reasons of its own, so a provider being resolvable
/// is not evidence that it holds the UI - only the entry point is.
/// </summary>
public class and_an_embedded_provider_holds_no_ui : Specification
{
    given.a_file_provider _webRoot;
    IFileProvider? _result;

    void Establish() => _webRoot = new(WorkbenchUI.EntryPoint);

    void Because() => _result = WorkbenchUI.Resolve(new given.a_file_provider("something-else.js"), _webRoot);

    [Fact] void should_serve_from_the_web_root_provider() => _result.ShouldEqual(_webRoot);
}
