// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.when_applying;

public class to_a_base_path : Specification
{
    given.an_index_page _inner;
    IFileProvider _result;

    void Establish() => _inner = new();

    void Because() => _result = WorkbenchBasePath.Apply(_inner, "/my-instance/chronicle");

    [Fact] void should_serve_the_entry_page_rewritten() => given.an_index_page.Read(_result.GetFileInfo(WorkbenchUI.EntryPoint)).ShouldContain("content=\"/my-instance/chronicle\"");
    [Fact] void should_report_the_length_of_the_rewritten_page() => _result.GetFileInfo(WorkbenchUI.EntryPoint).Length.ShouldEqual(System.Text.Encoding.UTF8.GetByteCount(given.an_index_page.Read(_result.GetFileInfo(WorkbenchUI.EntryPoint))));
    [Fact] void should_serve_other_files_untouched() => given.an_index_page.Read(_result.GetFileInfo("assets/app.js")).ShouldEqual("console.log('\"/\"')");
}
