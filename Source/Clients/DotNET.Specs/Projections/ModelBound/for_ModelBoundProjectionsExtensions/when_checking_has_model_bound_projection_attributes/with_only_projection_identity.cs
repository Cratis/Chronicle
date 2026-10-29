// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions.when_checking_has_model_bound_projection_attributes;

public class with_only_projection_identity : Specification
{
    bool _identityOnly;
    bool _identityAndPassive;
    bool _structIdentity;

    void Because()
    {
        _identityOnly = typeof(IdentityOnly).HasModelBoundProjectionAttributes();
        _identityAndPassive = typeof(IdentityAndPassive).HasModelBoundProjectionAttributes();
        _structIdentity = typeof(StructIdentity).HasModelBoundProjectionAttributes();
    }

    [Fact] void should_not_discover_an_identity_only_model() => _identityOnly.ShouldBeFalse();
    [Fact] void should_not_turn_passive_into_a_mapping_signal() => _identityAndPassive.ShouldBeFalse();
    [Fact] void should_not_discover_an_identity_only_record_struct() => _structIdentity.ShouldBeFalse();

    [Projection("identity-only")]
    public record IdentityOnly;

    [Projection("identity-passive")]
    [Passive]
    public record IdentityAndPassive;

    [Projection("identity-struct")]
    public readonly record struct StructIdentity;
}
