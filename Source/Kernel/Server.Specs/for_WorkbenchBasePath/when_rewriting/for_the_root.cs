// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.when_rewriting;

/// <summary>
/// The Cratis Stage keys its own rewrite on the page exactly as it ships, so serving the root must not touch it.
/// </summary>
public class for_the_root : Specification
{
    string _result;

    void Because() => _result = WorkbenchBasePath.Rewrite(given.an_index_page.Page, string.Empty);

    [Fact] void should_leave_the_page_exactly_as_it_ships() => _result.ShouldEqual(given.an_index_page.Page);
}
