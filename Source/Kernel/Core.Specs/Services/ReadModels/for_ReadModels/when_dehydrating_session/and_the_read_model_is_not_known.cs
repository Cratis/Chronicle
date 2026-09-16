// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_dehydrating_session;

public class and_the_read_model_is_not_known : given.all_dependencies
{
    Exception _result;

    void Establish() => _readModel.GetDefinition().Returns(default(Concepts.ReadModels.ReadModelDefinition)!);

    async Task Because() => _result = await Catch.Exception(() => _service.DehydrateSession(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = "unknown-read-model",
        EventSequenceId = "event-log",
        ReadModelKey = "read-model-key",
        SessionId = "9667b267-878e-4f57-a964-94fd29c238fa"
    }));

    [Fact] void should_report_the_missing_definition() => _result.ShouldBeOfExactType<ReadModelNotFound>();
}
