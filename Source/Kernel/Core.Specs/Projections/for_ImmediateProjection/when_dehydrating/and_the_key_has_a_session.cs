// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_dehydrating;

public class and_the_key_has_a_session : given.a_session_immediate_projection
{
    Task Because() => _grain.Dehydrate();

    [Fact] void should_deactivate_the_session() => _grain.SessionDeactivated.ShouldBeTrue();
}
