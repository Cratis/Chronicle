// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.for_StorageAuthorizationDiagnostic;

public class when_getting_all_diagnostics : Specification
{
    IStorageAuthorizationDiagnostics _diagnostics;
    IEnumerable<StorageAuthorizationDiagnostic> _result;

    void Establish()
    {
        _diagnostics = Substitute.For<IStorageAuthorizationDiagnostics>();
        _diagnostics.Failures.Returns([new StorageAuthorizationFailure("some-database", "some-collection", "createIndexes")]);
    }

    void Because() => _result = StorageAuthorizationDiagnostic.AllStorageAuthorizationDiagnostics(_diagnostics);

    [Fact] void should_return_its_database_collection_and_operation() =>
        _result.ShouldContainOnly([new StorageAuthorizationDiagnostic("some-database", "some-collection", "createIndexes")]);
}
