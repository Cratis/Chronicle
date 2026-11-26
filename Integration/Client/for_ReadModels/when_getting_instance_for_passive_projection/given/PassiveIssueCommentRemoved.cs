// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.given;

[EventType]
public record PassiveIssueCommentRemoved(PassiveCommentId Comment);
