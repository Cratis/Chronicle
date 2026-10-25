// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Warm joined and parent/child state with unrelated documents and two handled join types.
/// </summary>
/// <param name="fixture">The Chronicle fixture.</param>
public class joined_and_parent_state(ChronicleFixture fixture) : content_mutation_projections(fixture)
{
    public const string FirstJoinedDocument = "first-joined-document";
    public const string SecondJoinedDocument = "second-joined-document";
    public const string UnrelatedJoinedDocument = "unrelated-joined-document";
    public const string ReferenceSource = "shared-reference-source-not-a-document";

    static readonly TimeSpan _joinTimeout = TimeSpanFactory.FromSeconds(30);

    public EventSequenceNumber ReferenceSequenceNumber;
    public EventSequenceNumber JoinedTail;
    public JoinedReadModel[] JoinedDocuments;

    public override IEnumerable<Type> EventTypes => base.EventTypes.Concat([typeof(CrossSourceDocumentLinked), typeof(CrossSourceReferenceChanged)]);
    public override IEnumerable<Type> Projections => base.Projections.Concat([typeof(JoinedProjection)]);

    async Task Establish()
    {
        await EventStore.Projections.GetHandlerFor<JoinedProjection>().WaitTillSubscribed(_joinTimeout);
        await AppendAndWait("first-join-source", new CrossSourceDocumentLinked(FirstJoinedDocument, ReferenceSource));
        await AppendAndWait("second-join-source", new CrossSourceDocumentLinked(SecondJoinedDocument, ReferenceSource));
        await AppendAndWait("unrelated-join-source", new CrossSourceDocumentLinked(UnrelatedJoinedDocument, "unrelated-reference"));
        ReferenceSequenceNumber = await AppendAndWait(ReferenceSource, new CrossSourceReferenceChanged(9));
        JoinedTail = await AppendAndWait("unrelated-reference", new CrossSourceReferenceChanged(7));
        var replay = await EventStore.Projections.Replay<JoinedProjection>();
        await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replay, _joinTimeout);
        await CaptureJoinedAmount(9);
        JoinedDocuments.Length.ShouldEqual(3);
    }

    protected async Task CaptureJoinedAmount(int expected)
    {
        await EventStore.Jobs.WaitForThereToBeNoJobs(_joinTimeout);
        var handler = EventStore.Projections.GetHandlerFor<JoinedProjection>();
        await handler.WaitTillActive(_joinTimeout);
        await handler.WaitTillReachesEventSequenceNumber(JoinedTail, _joinTimeout);
        using var cancellation = new CancellationTokenSource(_joinTimeout);
        while (true)
        {
            JoinedDocuments = (await EventStore.ReadModels.GetInstances<JoinedReadModel>()).ToArray();
            if (JoinedDocuments.SingleOrDefault(model => model.Id == FirstJoinedDocument)?.Amount == expected &&
                JoinedDocuments.SingleOrDefault(model => model.Id == SecondJoinedDocument)?.Amount == expected)
            {
                return;
            }

            await Task.Delay(50, cancellation.Token);
        }
    }

    async Task<EventSequenceNumber> AppendAndWait(EventSourceId source, object @event)
    {
        var result = await EventStore.EventLog.Append(source, @event);
        var completion = await result.WaitForCompletion(_joinTimeout);
        completion.IsSuccess.ShouldBeTrue();
        return result.SequenceNumber;
    }
}
