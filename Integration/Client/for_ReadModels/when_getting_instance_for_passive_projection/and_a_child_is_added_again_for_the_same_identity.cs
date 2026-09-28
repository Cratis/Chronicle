// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.and_a_child_is_added_again_for_the_same_identity.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection;

/// <summary>
/// A child event arriving again for a child the instance already holds is an update of that child. It must
/// neither fail the read for that instance nor break the projection for every other instance read after it.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_a_child_is_added_again_for_the_same_identity(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public const string RepeatedSource = "issue-with-a-repeated-child";
        public const string OtherSource = "issue-read-afterwards";

        public PassiveCommentAssociations Repeated;
        public PassiveCommentAssociations Other;

        public override IEnumerable<Type> EventTypes => [typeof(PassiveCommentAssociated)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(PassiveCommentAssociations)];

        async Task Because()
        {
            await EventStore.EventLog.Append(RepeatedSource, new PassiveCommentAssociated("comment-1", PassiveProvider.GitHub, "100"));
            await EventStore.EventLog.Append(RepeatedSource, new PassiveCommentAssociated("comment-2", PassiveProvider.GitHub, "200"));
            await EventStore.EventLog.Append(RepeatedSource, new PassiveCommentAssociated("comment-1", PassiveProvider.GitHub, "101"));
            await EventStore.EventLog.Append(OtherSource, new PassiveCommentAssociated("comment-3", PassiveProvider.GitHub, "300"));

            Repeated = await EventStore.ReadModels.GetInstanceById<PassiveCommentAssociations>(RepeatedSource);
            Other = await EventStore.ReadModels.GetInstanceById<PassiveCommentAssociations>(OtherSource);
        }
    }

    [Fact] void should_return_the_instance_with_the_repeated_child() => Context.Repeated.ShouldNotBeNull();
    [Fact] void should_hold_each_child_once() => Context.Repeated.Associations.Select(_ => _.Id).ShouldContainOnly("comment-1", "comment-2");
    [Fact] void should_update_the_repeated_child() => Context.Repeated.Associations.Single(_ => _.Id == "comment-1").Reference.ShouldEqual("101");
    [Fact] void should_still_return_an_instance_read_afterwards() => Context.Other.ShouldNotBeNull();
    [Fact] void should_project_the_instance_read_afterwards() => Context.Other.Associations.Single().Reference.ShouldEqual("300");
}

public enum PassiveProvider
{
    None = 0,
    GitHub = 1
}

[EventType]
public record PassiveCommentAssociated(string Comment, PassiveProvider Provider, string Reference);

public record PassiveCommentAssociation(string Id, PassiveProvider Provider, string Reference);

[Passive]
public record PassiveCommentAssociations(
    string Id,
    [ChildrenFrom<PassiveCommentAssociated>(key: nameof(PassiveCommentAssociated.Comment), identifiedBy: nameof(PassiveCommentAssociation.Id))]
    IEnumerable<PassiveCommentAssociation> Associations);

#pragma warning restore SA1402
