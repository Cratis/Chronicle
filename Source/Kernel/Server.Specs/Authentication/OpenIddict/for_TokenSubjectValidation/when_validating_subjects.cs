// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict.for_TokenSubjectValidation;

public class when_validating_subjects : Specification
{
    [Fact] void should_accept_a_stable_application_id() => TokenSubjectValidation.TryGetApplicationId(Guid.NewGuid().ToString(), out _).ShouldBeTrue();
    [Fact] void should_reject_an_empty_application_id() => TokenSubjectValidation.TryGetApplicationId(Guid.Empty.ToString(), out _).ShouldBeFalse();
    [Fact] void should_reject_an_invalid_application_id() => TokenSubjectValidation.TryGetApplicationId("not-an-id", out _).ShouldBeFalse();
    [Fact] void should_accept_a_stable_user_id() => TokenSubjectValidation.IsStableUserId((UserId)Guid.NewGuid()).ShouldBeTrue();
    [Fact] void should_reject_an_empty_user_id() => TokenSubjectValidation.IsStableUserId(UserId.NotSet).ShouldBeFalse();
}
