// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexes;

public class when_checking_array_paths_form_a_chain : Specification
{
    [Fact] void should_accept_a_nested_single_path() => ReadModelIndexPath.ArrayPathsFormAChain(["orders", "orders.lines"]).ShouldBeTrue();
    [Fact] void should_accept_a_nested_path_with_a_top_level_scalar() => ReadModelIndexPath.ArrayPathsFormAChain(["orders", "orders.lines", "orders"]).ShouldBeTrue();
    [Fact] void should_accept_two_paths_under_one_array() => ReadModelIndexPath.ArrayPathsFormAChain(["lines", "lines"]).ShouldBeTrue();
    [Fact] void should_accept_outer_plus_inner_chain() => ReadModelIndexPath.ArrayPathsFormAChain(["orders", "orders", "orders.lines"]).ShouldBeTrue();
    [Fact] void should_refuse_parallel_arrays() => ReadModelIndexPath.ArrayPathsFormAChain(["lines", "tags"]).ShouldBeFalse();
    [Fact] void should_refuse_the_prefix_name_trap() => ReadModelIndexPath.ArrayPathsFormAChain(["line", "lines"]).ShouldBeFalse();
}
