// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.when_rewriting;

public class for_a_base_path : Specification
{
    string _result;

    void Because() => _result = WorkbenchBasePath.Rewrite(given.an_index_page.Page, "/my-instance/chronicle");

    [Fact] void should_write_the_base_path_into_the_meta_tag() => _result.ShouldContain("name=\"base-path\" content=\"/my-instance/chronicle\"");
    [Fact] void should_prefix_script_references() => _result.ShouldContain("src=\"/my-instance/chronicle/assets/app.js\"");
    [Fact] void should_prefix_stylesheet_references() => _result.ShouldContain("href=\"/my-instance/chronicle/assets/app.css\"");
    [Fact] void should_leave_no_root_absolute_reference() => _result.ShouldNotContain("src=\"/assets");
}
