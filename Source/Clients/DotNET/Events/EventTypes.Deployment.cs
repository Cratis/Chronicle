// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Events;

public partial class EventTypes
{
    /// <summary>
    /// Builds diagnostic payloads without changing this registry's schemas, migrators or failure state.
    /// </summary>
    /// <returns>An independently built registration payload.</returns>
    internal async Task<RegisterEventTypesRequest> BuildDeploymentPayloadSnapshot()
    {
        await _schemaVerdict;
        _payloadFailure?.Throw();
        var artifacts = CurrentArtifacts;
        var discovery = CaptureDiscovery();
        var detached = new EventTypes(_eventStore, _jsonSchemaGenerator, artifacts, _eventTypeMigrators, _enableEventTypeGenerationValidation, _namingPolicy)
        {
            _descriptors = discovery.Descriptors,
            _typesByEventType = discovery.TypesByEventType,
            _schemasByEventType = discovery.Schemas,
            _migrationServices = _migrationServices
        };
        detached._storeMigrators = StoreEventTypeMigrators.Bind(_eventTypeMigrators, artifacts, detached, _migrationServices, _namingPolicy);
        return detached.BuildRegistrationPayload();
    }
}
