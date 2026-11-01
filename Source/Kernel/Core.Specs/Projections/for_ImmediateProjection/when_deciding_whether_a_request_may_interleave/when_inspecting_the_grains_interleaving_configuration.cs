// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_deciding_whether_a_request_may_interleave;

public class when_inspecting_the_grains_interleaving_configuration : Specification
{
    MayInterleaveAttribute _attribute;

    void Because() => _attribute = typeof(ImmediateProjection).GetCustomAttribute<MayInterleaveAttribute>()!;

    [Fact] void should_declare_the_predicate_on_the_grain() => _attribute.CallbackMethodName.ShouldEqual(nameof(ImmediateProjection.IsDefinitionChangeNotification));
}
