// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concept_collections_with_conflicting_scopes : given.a_json_schema_generator_with_protection_support
{
    [Encrypted]
    record SubjectSecret(string Value) : ConceptAs<string>(Value);

    [Encrypted(EncryptionScope.Global)]
    record GlobalSecret(string Value) : ConceptAs<string>(Value);

    record GlobalArray([property: Encrypted(EncryptionScope.Global)] SubjectSecret[] Secrets);
    record GlobalList([property: Encrypted(EncryptionScope.Global)] List<SubjectSecret> Secrets);
    record SubjectArray([property: Encrypted] GlobalSecret[] Secrets);
    record SubjectList([Encrypted] List<GlobalSecret> Secrets);

    Exception _globalArrayError;
    Exception _globalListError;
    Exception _subjectArrayError;
    Exception _subjectListError;

    void Because()
    {
        _globalArrayError = Catch.Exception(() => _generator.Generate(typeof(GlobalArray)));
        _globalListError = Catch.Exception(() => _generator.Generate(typeof(GlobalList)));
        _subjectArrayError = Catch.Exception(() => _generator.Generate(typeof(SubjectArray)));
        _subjectListError = Catch.Exception(() => _generator.Generate(typeof(SubjectList)));
    }

    [Fact] void should_reject_global_encryption_on_a_subject_array() => _globalArrayError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_global_encryption_on_a_subject_list() => _globalListError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_subject_encryption_on_a_global_array() => _subjectArrayError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_subject_encryption_on_a_global_list_parameter() => _subjectListError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
}
