// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using FluentValidation.Results;

namespace Cratis.Chronicle.Sequences.for_ReopenStreamScope.when_validating;

public class and_reason_is_missing : Specification
{
    ValidationResult _result;

    async Task Because()
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "test")));
        _result = await new ReopenStreamScopeValidator(accessor).ValidateAsync(new ReopenStreamScope("store", "namespace", "event-log", " ", EventSourceId: "source"));
    }

    [Fact] void should_reject_the_repair() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_require_a_reason() => _result.Errors.Select(error => error.PropertyName).ShouldContain(nameof(ReopenStreamScope.Reason));
}
