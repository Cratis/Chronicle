// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Interleaved sources contribute to coupled documents and independent per-source totals.
/// </summary>
/// <param name="fixture">The Chronicle fixture.</param>
public class content_mutation_projections(ChronicleFixture fixture) : Specification(fixture)
{
    public const string FirstSource = "first-source";
    public const string SecondSource = "second-source";
    public const string NeighborSource = "neighbor-source";
    public const string SharedDocument = "shared-document-not-a-source";
    public const string NeighborDocument = "neighbor-document-not-a-source";

    static readonly TimeSpan _timeout = TimeSpanFactory.FromSeconds(30);

    public EventSequenceNumber MutatedSequenceNumber;
    public EventSequenceNumber Tail;
    public CoupledContentMutationDocument[] CoupledDocuments;
    public ContentMutationChildrenDocument[] ChildrenDocuments;
    public IsolatedContentMutationDocument[] IsolatedDocuments;

    public override IEnumerable<Type> EventTypes => [typeof(ContentContributionRecorded)];
    public override IEnumerable<Type> Projections =>
        [typeof(CoupledContentMutationProjection), typeof(ContentMutationChildrenProjection), typeof(IsolatedContentMutationProjection)];

    async Task Establish()
    {
        await EventStore.Projections.GetHandlerFor<CoupledContentMutationProjection>().WaitTillSubscribed(_timeout);
        await EventStore.Projections.GetHandlerFor<ContentMutationChildrenProjection>().WaitTillSubscribed(_timeout);
        await EventStore.Projections.GetHandlerFor<IsolatedContentMutationProjection>().WaitTillSubscribed(_timeout);

        await AppendAndWait(FirstSource, SharedDocument, 10);
        await AppendAndWait(SecondSource, SharedDocument, 20);
        MutatedSequenceNumber = await AppendAndWait(FirstSource, SharedDocument, 3);
        Tail = await AppendAndWait(NeighborSource, NeighborDocument, 7);
        var coupledReplay = await EventStore.Projections.Replay<CoupledContentMutationProjection>();
        await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(coupledReplay, _timeout);
        var childrenReplay = await EventStore.Projections.Replay<ContentMutationChildrenProjection>();
        await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(childrenReplay, _timeout);
        await EventStore.Jobs.WaitForThereToBeNoJobs(_timeout);
        await ReadDocuments();

        CoupledDocuments.Single(model => model.Id == SharedDocument).Total.ShouldEqual(33);
        ChildrenDocuments.Single(model => model.Id == SharedDocument).Items.Select(child => (child.Id, child.Amount))
            .OrderBy(child => child.Id).ShouldEqual([(FirstSource, 3), (SecondSource, 20)]);
        IsolatedDocuments.Single(model => model.Id == FirstSource).Total.ShouldEqual(13);
    }

    protected Task CaptureAfterMutation(int sharedTotal, int childAmount, int isolatedTotal, bool sourceRemoved = false) => CaptureWhen(() =>
    {
        var shared = CoupledDocuments.SingleOrDefault(model => model.Id == SharedDocument);
        var parent = ChildrenDocuments.SingleOrDefault(model => model.Id == SharedDocument);
        var first = IsolatedDocuments.SingleOrDefault(model => model.Id == FirstSource);
        var child = parent?.Items.SingleOrDefault(item => item.Id == FirstSource);
        return shared?.Total == sharedTotal && parent is not null &&
            (sourceRemoved ? child is null && first is null : child?.Amount == childAmount && first?.Total == isolatedTotal);
    });

    protected async Task CaptureWhen(Func<bool> completed)
    {
        await EventStore.Jobs.WaitForThereToBeNoJobs(_timeout);
        var coupled = EventStore.Projections.GetHandlerFor<CoupledContentMutationProjection>();
        var children = EventStore.Projections.GetHandlerFor<ContentMutationChildrenProjection>();
        var isolated = EventStore.Projections.GetHandlerFor<IsolatedContentMutationProjection>();
        await coupled.WaitTillActive(_timeout);
        await children.WaitTillActive(_timeout);
        await isolated.WaitTillActive(_timeout);
        await coupled.WaitTillReachesEventSequenceNumber(Tail, _timeout);
        await children.WaitTillReachesEventSequenceNumber(Tail, _timeout);
        await isolated.WaitTillReachesEventSequenceNumber(Tail, _timeout);

        using var cancellation = new CancellationTokenSource(_timeout);
        while (true)
        {
            await ReadDocuments();
            if (completed())
            {
                return;
            }

            await Task.Delay(50, cancellation.Token);
        }
    }

    async Task<EventSequenceNumber> AppendAndWait(EventSourceId source, string document, int amount)
    {
        var result = await EventStore.EventLog.Append(source, new ContentContributionRecorded(document, amount));
        var completion = await result.WaitForCompletion(_timeout);
        completion.IsSuccess.ShouldBeTrue();
        return result.SequenceNumber;
    }

    async Task ReadDocuments()
    {
        CoupledDocuments = (await EventStore.ReadModels.GetInstances<CoupledContentMutationDocument>()).ToArray();
        ChildrenDocuments = (await EventStore.ReadModels.GetInstances<ContentMutationChildrenDocument>()).ToArray();
        IsolatedDocuments = (await EventStore.ReadModels.GetInstances<IsolatedContentMutationDocument>()).ToArray();
    }
}
