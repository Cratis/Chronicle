// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_a_composite_key_without_a_schema_type : given.a_language_service_with_schemas<given.Model>
{
    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    string _generated;
    CompilerErrors _errors;

    void Because()
    {
        var compiled = _languageService.Compile("projection Order => Model\n  from UserAdded", Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var eventType = compiled.From.Keys.Single();
        compiled.From[eventType] = compiled.From[eventType] with { Key = "$composite(first=userId,second=name)" };
        _generated = _languageService.Generate(compiled, _readModelDefinition);
        _errors = _languageService.Compile(_generated, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(_ => CompilerErrors.Empty, errors => errors);
    }

    [Fact] void should_emit_the_documented_placeholder() => _generated.ShouldContain("key CompositeKey");
    [Fact] void should_report_the_missing_type_on_recompile() => _errors.Errors.ShouldContain(_ => _.Message.Contains("Composite key type 'CompositeKey' not found", StringComparison.Ordinal));
}
