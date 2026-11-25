// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_flat_keys_have_unsupported_scopes : given.a_runtime_language_service
{
    [Theory]
    [InlineData("every\n    increment age", "'EverySyntax' is not supported in a projection identified by a flat composite key")]
    [InlineData("all\n    increment age", "'AllSyntax' is not supported in a projection identified by a flat composite key")]
    [InlineData("join Changed on name\n    with Changed\n      name = name", "'JoinSyntax' is not supported in a projection identified by a flat composite key")]
    [InlineData("children items identified by id\n    from Changed\n      id = name", "'ChildrenSyntax' is not supported in a projection identified by a flat composite key")]
    [InlineData("nested summary\n    from Changed\n      name = name", "'NestedSyntax' is not supported in a projection identified by a flat composite key")]
    [InlineData("clear with Removed", "'clear with' is only valid inside a nested block")]
    [InlineData("remove via join on Removed key name", "'remove via join' at the root is not supported")]
    public void should_refuse_the_scope_in_a_flat_root(string block, string expectedDiagnostic)
    {
        var source = "projection Alerts => Alert\n  from Changed\n    key\n      name = name\n      age = age\n  " + block + "\n";
        var result = _language.Compile(source, ProjectionOwner.Client, [_readModel], _schemas);
        var diagnostics = result.Match(_ => string.Empty, errors => string.Join("; ", errors.Errors.Select(error => error.Message)));
        diagnostics.ShouldContain(expectedDiagnostic);
    }

    [Fact]
    void should_validate_flat_parts_against_the_inferred_schema()
    {
        const string source = "projection Alerts\n  from Changed\n    key\n      name = name\n      missing = age\n";
        var result = _language.Compile(source, ProjectionOwner.Client, [], _schemas);
        result.Match(_ => false, errors => errors.Errors.Any(error => error.Message.Contains("Property 'missing' not found", StringComparison.Ordinal))).ShouldBeTrue();
    }
}
