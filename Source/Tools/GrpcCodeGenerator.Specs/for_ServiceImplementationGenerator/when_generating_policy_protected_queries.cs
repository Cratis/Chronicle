// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis;
using TestAssembly.AdministratorPolicies;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator;

public class when_generating_policy_protected_queries : given.a_generated_service
{
    IReadOnlyList<Diagnostic> _errors;
    Assembly? _assembly;
    GeneratedService _generated;

    void Establish()
    {
        _serviceDefinition = new ServiceDefinition("Administrators", "TestAssembly.AdministratorPolicies");
        _serviceDefinition.Commands.Add(new CommandDefinition(typeof(ProtectedCommand)));
        _serviceDefinition.Queries.Add(new QueryDefinition(typeof(ProtectedAdministrator),
            [new QueryMethodDefinition(typeof(ProtectedAdministrator).GetMethod(nameof(ProtectedAdministrator.AllAdministrators))!)]));
        _serviceDefinition.Queries.Add(new QueryDefinition(typeof(MethodProtectedAdministrator),
        [
            new QueryMethodDefinition(typeof(MethodProtectedAdministrator).GetMethod(nameof(MethodProtectedAdministrator.FindAdministrators))!),
            new QueryMethodDefinition(typeof(MethodProtectedAdministrator).GetMethod(nameof(MethodProtectedAdministrator.UnprotectedQuery))!)
        ]));
        _contractCode = new ServiceInterfaceGenerator(0, ContractsNamespace).Generate(_serviceDefinition, _outputDirectory);
    }

    void Because()
    {
        _generated = new ServiceImplementationGenerator(0, ContractsNamespace, ImplementationsNamespace).Generate(_serviceDefinition, _outputDirectory);
        (_errors, _assembly) = given.GeneratedSourceCompiler.Compile(_contractCode, File.ReadAllText(_generated.Path));
    }

    [Fact] void should_compile_the_implementation_with_its_contract() => _errors.ShouldBeEmpty();
    [Fact] void should_copy_the_arc_read_model_policy_to_the_transport_method() => Policy("AllAdministrators").ShouldEqual("Chronicle.Administrator");
    [Fact] void should_copy_the_arc_method_policy_to_the_transport_method() => Policy("FindAdministrators").ShouldEqual("Chronicle.Administrator");
    [Fact] void should_leave_other_queries_unchanged() => Policy("UnprotectedQuery").ShouldBeNull();
    [Fact] void should_leave_commands_to_the_resource_aware_command_pipeline() => Policy("ProtectedCommand").ShouldBeNull();

    string? Policy(string method) => _assembly!.GetType(_generated.TypeName.Replace("global::", string.Empty, StringComparison.Ordinal))!
        .GetMethod(method)!.GetCustomAttributes<AuthorizeAttribute>().SingleOrDefault()?.Policy;
}
