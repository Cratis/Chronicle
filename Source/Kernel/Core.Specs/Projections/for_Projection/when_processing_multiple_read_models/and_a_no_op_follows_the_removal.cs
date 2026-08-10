// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_no_op_follows_the_removal : given.a_projection_sequence_ending_after_removal
{
    IEnumerable<ExpandoObject> _result;

    void Establish() => ConfigureNoOpAfterRemoval();

    async Task Because() => _result = await ProcessForMultipleReadModels(CreatedEvent, RemovedEvent, NoOpEvent);

    [Fact] void should_preserve_the_removal() => _result.ShouldBeEmpty();
}
