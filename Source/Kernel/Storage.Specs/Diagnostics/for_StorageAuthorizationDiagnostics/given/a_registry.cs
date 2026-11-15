// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.given;

public class a_registry : Specification
{
    protected RecordingDiagnosticsLogger _logger;
    protected StorageAuthorizationDiagnostics _registry;
    protected StorageAuthorizationFailure _failure = new("System", "EventTypeStatistics", "listIndexes");

    void Establish()
    {
        _logger = new();
        _registry = new(_logger);
    }
}
