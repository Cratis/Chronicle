// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Decisions;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

public class when_getting_instance_for_decision : given.all_dependencies
{
    class MyModel
    {
        public string Name { get; set; } = string.Empty;
    }

    readonly EventType _created = new("Created", EventTypeGeneration.First);
    readonly EventType _removed = new("Removed", EventTypeGeneration.First);

    [Fact]
    public async Task should_return_the_instance_and_a_scope_with_the_server_projected_types()
    {
        _decisionReadModelsService.GetInstanceForDecision(Arg.Any<GetInstanceForDecisionRequest>())
            .Returns(QueryResult<DecisionReadModelResponse>.Success(Guid.NewGuid(), new()
            {
                Instance = "{\"Name\":\"present\"}",
                SequenceNumber = 7,
                EventTypes = [_created.ToString(), _removed.ToString()]
            }));

        var read = await ((IReadModels)_readModels).GetInstanceForDecision<MyModel>("source");
        read.Instance!.Name.ShouldEqual("present");
        read.ToConcurrencyScope().SequenceNumber.ShouldEqual((EventSequenceNumber)7);
        read.ToConcurrencyScope().EventTypes.ShouldContain(_removed);
        await _decisionReadModelsService.Received(1).GetInstanceForDecision(Arg.Is<GetInstanceForDecisionRequest>(request =>
            request.Key == "source" && request.ReadModelIdentifier == typeof(MyModel).GetReadModelIdentifier().Value));
    }

    [Fact]
    public async Task should_return_exact_absence_after_removal()
    {
        _decisionReadModelsService.GetInstanceForDecision(Arg.Any<GetInstanceForDecisionRequest>())
            .Returns(QueryResult<DecisionReadModelResponse>.Success(Guid.NewGuid(), new()
            {
                Instance = "null",
                SequenceNumber = 9,
                EventTypes = [_created.ToString(), _removed.ToString()]
            }));

        var read = await ((IReadModels)_readModels).GetInstanceForDecision<MyModel>("source");
        read.Instance.ShouldBeNull();
        read.ToConcurrencyScope().SequenceNumber.ShouldEqual((EventSequenceNumber)9);
    }

    [Fact]
    public async Task should_fail_closed_when_the_response_has_no_data()
    {
        _decisionReadModelsService.GetInstanceForDecision(Arg.Any<GetInstanceForDecisionRequest>())
            .Returns(QueryResult<DecisionReadModelResponse>.Success(Guid.NewGuid(), null!));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IReadModels)_readModels).GetInstanceForDecision<MyModel>("source"));
    }

    [Fact]
    public async Task should_preserve_the_typed_refusal()
    {
        _decisionReadModelsService.GetInstanceForDecision(Arg.Any<GetInstanceForDecisionRequest>())
            .Returns(QueryResult<DecisionReadModelResponse>.Success(Guid.NewGuid(), new()
            {
                Refusal = DecisionReadRefusal.Join
            }));

        var refusal = await Assert.ThrowsAsync<DecisionReadRefused>(() =>
            ((IReadModels)_readModels).GetInstanceForDecision<MyModel>("source"));
        refusal.Reason.ShouldEqual(DecisionReadRefusal.Join);
    }
}
