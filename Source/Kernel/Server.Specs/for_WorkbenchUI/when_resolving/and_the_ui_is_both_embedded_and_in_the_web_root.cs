// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.when_resolving;

public class and_the_ui_is_both_embedded_and_in_the_web_root : Specification
{
    const string EmbeddedOnlyFile = "embedded-only.js";
    const string WebRootOnlyFile = "web-root-only.js";

    IFileProvider? _result;

    void Because() => _result = WorkbenchUI.Resolve(
        new given.a_file_provider(WorkbenchUI.EntryPoint, EmbeddedOnlyFile),
        new given.a_file_provider(WorkbenchUI.EntryPoint, WebRootOnlyFile));

    [Fact] void should_serve_the_entry_point() => _result!.GetFileInfo(WorkbenchUI.EntryPoint).Exists.ShouldBeTrue();
    [Fact] void should_serve_files_only_the_embedded_provider_holds() => _result!.GetFileInfo(EmbeddedOnlyFile).Exists.ShouldBeTrue();
    [Fact] void should_serve_files_only_the_web_root_provider_holds() => _result!.GetFileInfo(WebRootOnlyFile).Exists.ShouldBeTrue();
}
