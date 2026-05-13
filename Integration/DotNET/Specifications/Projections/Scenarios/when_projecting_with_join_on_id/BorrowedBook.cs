// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_on_id;

public record BorrowedBook(
    Guid Id,
    Guid UserId,
    string Title,
    string User,
    DateTimeOffset Borrowed,
    DateTimeOffset Returned,
    EventSequenceNumber __lastHandledEventSequenceNumber);
