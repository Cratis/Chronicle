// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_inspecting_the_token_endpoint : Specification
{
    [Fact]
    void should_explicitly_ignore_antiforgery_validation() =>
        typeof(AuthorizationController)
            .GetMethod(nameof(AuthorizationController.Exchange))!
            .IsDefined(typeof(IgnoreAntiforgeryTokenAttribute), inherit: true)
            .ShouldBeTrue();
}
