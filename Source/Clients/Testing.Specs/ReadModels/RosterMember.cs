// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child entry on a <see cref="MembershipRoster"/>, keyed by a <see cref="MemberId"/> concept. Its
/// <see cref="MemberName"/> is joined in from a separate member stream via <see cref="MemberProfileCreated"/>.
/// </summary>
/// <param name="MemberId">The member identifier, used as the child key and join key.</param>
/// <param name="MemberName">The member name, joined in from <see cref="MemberProfileCreated"/>.</param>
public record RosterMember(
    MemberId MemberId,
    string MemberName);
