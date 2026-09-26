// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class structural_levels : for_LanguageService.given.a_language_service
{
    CompilerErrors _duplicateNestedFrom;
    CompilerErrors _duplicateClear;
    CompilerErrors _removeAndClear;
    CompilerErrors _duplicateJoinAcrossBlocks;
    ProjectionDefinition? _reusedParentEvent;

    void Because()
    {
        _duplicateNestedFrom = Errors("""
            projection Test => CompanyReadModel
              nested settings
                from DepartmentCreated
                from DepartmentCreated
            """);
        _duplicateClear = Errors("""
            projection Test => CompanyReadModel
              nested settings
                from DepartmentCreated
                clear with DepartmentDeleted
                clear with DepartmentDeleted
            """);
        _removeAndClear = Errors("""
            projection Test => CompanyReadModel
              nested settings
                from DepartmentCreated
                remove with DepartmentDeleted
                clear with DepartmentDeleted
            """);
        _duplicateJoinAcrossBlocks = Errors("""
            projection Test => CompanyReadModel
              join Department on id
                with DepartmentCreated
              join Department on id
                with DepartmentCreated
            """);
        var reused = _languageService.Compile("""
            projection Test => CompanyReadModel
              from DepartmentCreated
              nested settings
                from DepartmentCreated
            """,
            ProjectionOwner.Client,
            [],
            []);
        _reusedParentEvent = reused.Match<ProjectionDefinition?>(_ => _, _ => null);
    }

    CompilerErrors Errors(string declaration) => _languageService.Compile(declaration, ProjectionOwner.Client, [], [])
        .Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_duplicate_from_inside_nested() => _duplicateNestedFrom.Errors.ShouldContain(_ => _.Message.Contains("Duplicate event type 'DepartmentCreated'", StringComparison.Ordinal));
    [Fact] void should_reject_duplicate_clear_with() => _duplicateClear.Errors.ShouldContain(_ => _.Message.Contains("Duplicate event type 'DepartmentDeleted'", StringComparison.Ordinal));
    [Fact] void should_reject_remove_with_and_clear_with_same_event() => _removeAndClear.Errors.ShouldContain(_ => _.Message.Contains("Duplicate event type 'DepartmentDeleted'", StringComparison.Ordinal));
    [Fact] void should_reject_join_event_in_separate_join_blocks() => _duplicateJoinAcrossBlocks.Errors.ShouldContain(_ => _.Message.Contains("Duplicate event type 'DepartmentCreated'", StringComparison.Ordinal));
    [Fact] void should_allow_parent_and_nested_to_handle_same_event() => _reusedParentEvent.ShouldNotBeNull();
}
