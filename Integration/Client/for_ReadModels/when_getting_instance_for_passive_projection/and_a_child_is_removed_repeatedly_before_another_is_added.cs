// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.given;

using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.and_a_child_is_removed_repeatedly_before_another_is_added.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection;

[Collection(ChronicleCollection.Name)]
public class and_a_child_is_removed_repeatedly_before_another_is_added(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : a_passive_issue_comments_projection(fixture)
    {
        async Task Establish() => await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentAdded(FirstComment, "Removed comment"));

        async Task Because()
        {
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentRemoved(FirstComment));
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentRemoved(FirstComment));
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentRemoved(FirstComment));
            await EventStore.EventLog.Append(IssueId, new PassiveIssueCommentAdded(SecondComment, "New comment"));

            Result = await EventStore.ReadModels.GetInstanceById<PassiveIssueComments>(IssueId);
        }
    }

    [Fact] void should_return_the_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_only_the_new_child() => Context.Result.Comments.Select(_ => _.Id).ShouldContainOnly(Context.SecondComment);
    [Fact] void should_preserve_the_new_child_body() => Context.Result.Comments.Single().Body.ShouldEqual("New comment");
}
