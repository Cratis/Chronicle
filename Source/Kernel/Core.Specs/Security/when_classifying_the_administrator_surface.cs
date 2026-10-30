// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Sequences.given;

namespace Cratis.Chronicle.Security;

public class when_classifying_the_administrator_surface : Specification
{
    static readonly Type[] _readModels = typeof(Administrator).Assembly.GetTypes()
        .Where(type => type.IsDefined(typeof(ReadModelAttribute), false)).ToArray();

    public static TheoryData<Type> Commands() => Cases(public_store_write_commands.AllTypes);
    public static TheoryData<Type> ReadModels() => Cases(_readModels);

    public static TheoryData<Type, string> RequiredPolicies()
    {
        var policies = new TheoryData<Type, string>();
        foreach (var pair in given.administrator_surface_policy.Required)
        {
            var artifact = typeof(Administrator).Assembly.GetType(pair.Key);

            // ResetKernelState is intentionally absent from non-DEVELOPMENT builds.
            if (artifact is null && pair.Key == "Cratis.Chronicle.DevelopmentTools.ResetKernelState") continue;
            artifact.ShouldNotBeNull();
            policies.Add(artifact, pair.Value);
        }
        return policies;
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void should_classify_every_command_with_a_policy_or_exemption_reason(Type command)
    {
        var required = given.administrator_surface_policy.Required.ContainsKey(command.FullName!);
        var guarded = system_store_command_policy.Guarded.Contains(command.FullName!);
        var exempt = given.administrator_surface_policy.ExemptCommands.TryGetValue(command.FullName!, out var reason);
        ((required ? 1 : 0) + (guarded ? 1 : 0) + (exempt ? 1 : 0)).ShouldEqual(1);
        if (exempt) string.IsNullOrWhiteSpace(reason).ShouldBeFalse();
        if (guarded) Policy(command).ShouldBeNull(); // P1 reserves System instead of granting administrators a public write bypass.
    }

    [Theory]
    [MemberData(nameof(ReadModels))]
    public void should_classify_every_query_surface_with_a_policy_or_exemption_reason(Type readModel)
    {
        var required = given.administrator_surface_policy.Required.ContainsKey(readModel.FullName!);
        var exempt = given.administrator_surface_policy.ExemptReadModels.TryGetValue(readModel.FullName!["Cratis.Chronicle.".Length..], out var reason);
        (required ^ exempt).ShouldBeTrue();
        if (!exempt) return;
        string.IsNullOrWhiteSpace(reason).ShouldBeFalse();
        Policy(readModel).ShouldBeNull();
        foreach (var method in readModel.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            Policy(method).ShouldBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(RequiredPolicies))]
    public void should_require_the_classified_policy(Type artifact, string policy) => Policy(artifact).ShouldEqual(policy);

    static TheoryData<Type> Cases(IEnumerable<Type> types)
    {
        var cases = new TheoryData<Type>();
        foreach (var type in types) cases.Add(type);
        return cases;
    }

    static string? Policy(MemberInfo artifact) => artifact.GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    [Fact] void should_inspect_a_nonempty_command_surface() => public_store_write_commands.AllTypes.Any().ShouldBeTrue();
    [Fact] void should_inspect_a_nonempty_query_surface() => _readModels.Length.ShouldBeGreaterThan(0);
}
