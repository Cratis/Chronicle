// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Compliance;

/// <summary>
/// Represents an implementation of <see cref="ICompliance"/>.
/// </summary>
/// <param name="jsonComplianceManager">The <see cref="IJsonComplianceManager"/> for handling compliance on JSON.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
internal sealed class ComplianceService(IJsonComplianceManager jsonComplianceManager, ILogger<ComplianceService> logger) : ICompliance
{
    /// <inheritdoc/>
    public async Task<ReleaseResponse> Release(ReleaseRequest request, CallContext context = default)
    {
        try
        {
            var schema = await JsonSchema.FromJsonAsync(request.Schema);
            var json = JsonNode.Parse(request.Payload)?.AsObject();

            if (json is null)
            {
                return new ReleaseResponse { HasError = true, Error = "Invalid JSON payload." };
            }

            var released = await jsonComplianceManager.Release(
                (EventStoreName)request.EventStore,
                (EventStoreNamespaceName)request.Namespace,
                schema,
                request.Subject,
                json);

            return new ReleaseResponse { Payload = released.ToJsonString() };
        }
        catch (JsonException ex)
        {
            logger.FailedToRelease(request.EventStore, request.Namespace, request.Subject, ex);
            return new ReleaseResponse { HasError = true, Error = ex.Message };
        }
        catch (InvalidOperationException ex)
        {
            logger.FailedToRelease(request.EventStore, request.Namespace, request.Subject, ex);
            return new ReleaseResponse { HasError = true, Error = ex.Message };
        }
    }
}
