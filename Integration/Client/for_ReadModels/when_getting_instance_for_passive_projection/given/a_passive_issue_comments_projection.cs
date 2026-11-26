// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.given;

public class a_passive_issue_comments_projection(ChronicleFixture fixture) : Specification(fixture)
{
    public const string IssueId = "b8728010-2d9f-4b44-970b-6f49df2b61c0";

    public PassiveCommentId FirstComment { get; } = new("3f2b1234-1234-1234-1234-123456789abc");
    public PassiveCommentId SecondComment { get; } = new("3f2b5678-5678-5678-5678-123456789abc");
    public PassiveCommentId ThirdComment { get; } = new("3f2b9012-9012-9012-9012-123456789abc");
    public PassiveIssueComments Result;

    public override IEnumerable<Type> EventTypes => [typeof(PassiveIssueCommentAdded), typeof(PassiveIssueCommentRemoved)];
    public override IEnumerable<Type> ModelBoundProjections => [typeof(PassiveIssueComments)];
}
