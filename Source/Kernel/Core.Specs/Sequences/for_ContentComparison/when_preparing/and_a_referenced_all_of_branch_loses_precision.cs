// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_ContentComparison.when_preparing;

public class and_a_referenced_all_of_branch_loses_precision : given.composed_storage_conversions
{
    async Task Establish() => await Configure(ReferencedSchema);

    void Because() => Compare();

    [Fact] void should_reject_loss_in_memory() => _results[0].ShouldBeNull();
    [Fact] void should_reject_loss_in_sql_serialization() => _results[1].ShouldBeNull();
    [Fact] void should_reject_loss_in_mongodb() => _results[2].ShouldBeNull();
    [Fact] void should_allow_lossless_memory_conversion() => _controls[0].ShouldNotBeNull();
    [Fact] void should_allow_lossless_sql_serialization() => _controls[1].ShouldNotBeNull();
    [Fact] void should_allow_lossless_mongodb_conversion() => _controls[2].ShouldNotBeNull();
}
