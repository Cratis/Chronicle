// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concepts_with_conflicting_encryption_scopes : given.a_json_schema_generator_with_protection_support
{
    [Encrypted]
    record SubjectSecret(string Value) : ConceptAs<string>(Value);

    [Encrypted(EncryptionScope.Global)]
    record GlobalSecret(string Value) : ConceptAs<string>(Value);

    record GlobalProperty([property: Encrypted(EncryptionScope.Global)] SubjectSecret Secret);
    record SubjectProperty([property: Encrypted] GlobalSecret Secret);
    record GlobalParameter([Encrypted(EncryptionScope.Global)] SubjectSecret Secret);
    record SubjectParameter([Encrypted] GlobalSecret Secret);

    Exception _globalPropertyError;
    Exception _subjectPropertyError;
    Exception _globalParameterError;
    Exception _subjectParameterError;

    void Because()
    {
        _globalPropertyError = Catch.Exception(() => _generator.Generate(typeof(GlobalProperty)));
        _subjectPropertyError = Catch.Exception(() => _generator.Generate(typeof(SubjectProperty)));
        _globalParameterError = Catch.Exception(() => _generator.Generate(typeof(GlobalParameter)));
        _subjectParameterError = Catch.Exception(() => _generator.Generate(typeof(SubjectParameter)));
    }

    [Fact] void should_reject_a_global_property_on_a_subject_concept() => _globalPropertyError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_a_subject_property_on_a_global_concept() => _subjectPropertyError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_a_global_parameter_on_a_subject_concept() => _globalParameterError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_a_subject_parameter_on_a_global_concept() => _subjectParameterError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_name_the_property_and_both_scopes() => _globalPropertyError.Message.ShouldEqual("'Secret' specifies conflicting [Encrypted] scopes: its type uses 'Subject' and its property or constructor parameter uses 'Global'. A value needs exactly one encryption scope; use the same scope on both declarations.");
}
