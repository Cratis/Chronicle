// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.given;

[Passive]
public record PassiveIssueComments(
    string Id,
    [ChildrenFrom<PassiveIssueCommentAdded>(key: nameof(PassiveIssueCommentAdded.Comment), identifiedBy: nameof(PassiveIssueComment.Id))]
    [RemovedWith<PassiveIssueCommentRemoved>(key: nameof(PassiveIssueCommentRemoved.Comment))]
    IEnumerable<PassiveIssueComment> Comments);
