// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingCommandFilter;

public class when_a_command_has_a_case_variant_namespace : Specification
{
    readonly RemoveNamespace _command = new("Studio", "cratis");
    NamespaceCasingCommandFilter _filter;

    void Establish()
    {
        var casing = Substitute.For<INamespaceCasing>();
        casing.Canonicalize("Studio", "cratis").Returns((EventStoreNamespaceName)"Cratis");
        _filter = new(new ServiceCollection().AddSingleton(casing).BuildServiceProvider());
    }

    async Task Because() => await _filter.OnExecution(new(CorrelationId.New(), typeof(RemoveNamespace), _command, [], new CommandContextValues()));

    [Fact] void should_mutate_the_command_to_the_stored_casing() => _command.Namespace.Value.ShouldEqual("Cratis");
}
