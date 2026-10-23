// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

record OwnerIssue(int Number, string State, string? StateReason, bool IsPullRequest);
