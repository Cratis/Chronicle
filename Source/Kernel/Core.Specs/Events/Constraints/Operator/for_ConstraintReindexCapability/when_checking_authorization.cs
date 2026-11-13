// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ConstraintReindexCapability;

public class when_checking_authorization : Specification
{
    [Fact] void should_be_a_query_surface() => typeof(ConstraintReindexCapability).IsDefined(typeof(ReadModelAttribute)).ShouldBeTrue();
    [Fact] void should_require_the_same_policy_as_reindexing() => typeof(ConstraintReindexCapability).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldEqual(typeof(ReindexConstraints).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
    [Fact] void should_require_administrator_operations() => typeof(ConstraintReindexCapability).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldEqual(WellKnownAuthorizationPolicies.AdministratorOperations);
    [Fact] void should_be_discoverable_as_a_query() => typeof(ConstraintReindexCapability).GetMethod(nameof(ConstraintReindexCapability.CanReindexConstraints))!.IsValidQueryFor(typeof(ConstraintReindexCapability)).ShouldBeTrue();
    [Fact] void should_allow_reindexing_after_authorization() => ConstraintReindexCapability.CanReindexConstraints().CanReindex.ShouldBeTrue();
}
