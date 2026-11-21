// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.given;

using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.and_children_with_guid_shaped_string_identities_are_removed.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection;

[Collection(ChronicleCollection.Name)]
public class and_children_with_guid_shaped_string_identities_are_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : a_passive_issue_comments_projection(fixture)
    {
        async Task Establish()
        {
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentAdded(FirstComment, "First comment"));
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentAdded(SecondComment, "Second comment"));
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentAdded(ThirdComment, "Remaining comment"));
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentRemoved(FirstComment));
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentRemoved(SecondComment));

            Result = await EventStore.ReadModels.GetInstanceById<PassiveIssueComments>(IssueId);
        }
    }

    [Fact] void should_return_the_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_only_the_remaining_child() => Context.Result.Comments.Select(_ => _.Id).ShouldContainOnly(Context.ThirdComment);
    [Fact] void should_preserve_the_remaining_child_body() => Context.Result.Comments.Single().Body.ShouldEqual("Remaining comment");
}
