// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies.given;

public class generation_policies : Specification
{
    protected EventStoreName _eventStore;
    protected ProjectionId _projectionId;
    protected ChronicleOptions _options;
    protected IGenerationPolicyOverrides _overrides;
    protected IGenerationPolicies _policies;
    protected GenerationPromotionPolicy _result;

    void Establish()
    {
        _eventStore = "orders";
        _projectionId = "order-summary";
        _options = new ChronicleOptions { Features = new() { ProjectionGenerations = true } };
        _overrides = Substitute.For<IGenerationPolicyOverrides>();
        var options = Substitute.For<IOptions<ChronicleOptions>>();
        options.Value.Returns(_ => _options);
        _policies = new GenerationPolicies(options, _overrides);
    }
}
