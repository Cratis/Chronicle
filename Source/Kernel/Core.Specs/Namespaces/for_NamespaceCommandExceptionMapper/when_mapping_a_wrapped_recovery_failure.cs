// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCommandExceptionMapper;

public class when_mapping_a_wrapped_recovery_failure : Specification
{
    CommandContext _context;
    CommandResult _result;
    bool _mapped;

    void Establish()
    {
        var command = new RemoveNamespace("store", "tenant");
        _context = new(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues());
    }

    void Because() => _mapped = new NamespaceCommandExceptionMapper().TryMap(
        _context,
        new TargetInvocationException(new NamespaceRecoveryRequired("private-tenant")),
        out _result!);

    [Fact] void should_map_the_typed_failure() => _mapped.ShouldBeTrue();
    [Fact] void should_preserve_correlation() => _result.CorrelationId.ShouldEqual(_context.CorrelationId);
    [Fact] void should_expose_only_the_literal_code() => _result.ExceptionMessages.ShouldEqual(["NamespaceRecoveryRequired"]);
}
