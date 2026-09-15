// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_read_model_is_not_known : given.all_dependencies
{
    Exception _result = null!;

    /// <summary>
    /// A lookup miss leaves the grain state unpopulated rather than returning a null definition, so every
    /// member access on it used to throw NullReferenceException out of the gRPC handler.
    /// </summary>
    void Establish() => _readModel.GetDefinition().Returns(default(Concepts.ReadModels.ReadModelDefinition)!);

    async Task Because() => _result = await Catch.Exception(() => _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = "not-a-registered-read-model",
        EventSequenceId = "event-log",
        ReadModelKey = "read-model-key"
    }));

    [Fact] void should_fail_with_a_meaningful_error() => _result.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_identifier_that_was_looked_up() => _result.Message.ShouldContain("not-a-registered-read-model");
}
