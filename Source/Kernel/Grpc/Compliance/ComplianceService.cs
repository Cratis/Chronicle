// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Compliance;

/// <summary>
/// Represents an implementation of <see cref="ICompliance"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for resolving the kernel <see cref="IPIIManager"/> grain.</param>
/// <param name="schemaMetadataManager">The <see cref="IJsonSchemaMetadataManager"/> for handling both compliance and security metadata on JSON.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
/// <param name="authorization">The compliance capability boundary.</param>
internal sealed class ComplianceService(IGrainFactory grainFactory, IJsonSchemaMetadataManager schemaMetadataManager, ILogger<ComplianceService> logger, ComplianceAuthorization authorization) : ICompliance
{
    /// <inheritdoc/>
    public async Task<ReleaseResponse> Release(ReleaseRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        await authorization.Release(request.EventStore, request.Namespace, context);
        try
        {
            var schema = await JsonSchema.FromJsonAsync(request.Schema);
            var json = JsonNode.Parse(request.Payload)?.AsObject();

            if (json is null)
            {
                return new ReleaseResponse { HasError = true, Error = "Invalid JSON payload.", Strict = request.Strict };
            }

            var released = await ReleaseContent(request, schema, json);
            return new ReleaseResponse { Payload = released.ToJsonString(), Strict = request.Strict };
        }
        catch (SchemaMetadataActionFailed ex) when (request.Strict)
        {
            return StrictFailure(ex, ex.PropertyPath);
        }
        catch (Exception ex) when (request.Strict)
        {
            return StrictFailure(ex);
        }
        catch (JsonException ex)
        {
            logger.FailedToRelease(request.EventStore, request.Namespace, ex.GetType().Name);
            return new ReleaseResponse { HasError = true, Error = ex.Message };
        }
        catch (InvalidOperationException ex)
        {
            logger.FailedToRelease(request.EventStore, request.Namespace, ex.GetType().Name);
            return new ReleaseResponse { HasError = true, Error = ex.Message };
        }
    }

    /// <inheritdoc/>
    public async Task DeleteEncryptionKey(DeleteEncryptionKeyRequest request, CallContext context = default)
    {
        var result = await DeleteEncryptionKeyWithCoverage(request, context);
        EnsureCovered(result.NotCoveredEventStores);
    }

    /// <inheritdoc/>
    public async Task AllowNewEncryptionKey(AllowNewEncryptionKeyRequest request, CallContext context = default)
    {
        var result = await AllowNewEncryptionKeyWithCoverage(request, context);
        EnsureCovered(result.NotCoveredEventStores);
    }

    /// <inheritdoc/>
    public async Task<DeleteEncryptionKeyResponse> DeleteEncryptionKeyWithCoverage(DeleteEncryptionKeyRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        var coverage = await authorization.KeyLifecycle("delete-key", request.EventStore, request.Namespace, context);
        if (coverage.Allowed.Count > 0)
        {
            await GetPIIManager(request.EventStore, request.Namespace).DeleteEncryptionKeyIn(request.Identifier, coverage.Allowed.ToArray());
        }

        EnsureListed(coverage);
        return new() { NotCoveredEventStores = coverage.NotCovered.Select(store => store.Value).ToArray() };
    }

    /// <inheritdoc/>
    public async Task<AllowNewEncryptionKeyResponse> AllowNewEncryptionKeyWithCoverage(AllowNewEncryptionKeyRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        var coverage = await authorization.KeyLifecycle("allow-new-key", request.EventStore, request.Namespace, context);
        if (coverage.Allowed.Count > 0)
        {
            await GetPIIManager(request.EventStore, request.Namespace).AllowNewEncryptionKeyIn(request.Identifier, coverage.Allowed.ToArray());
        }

        EnsureListed(coverage);
        return new() { NotCoveredEventStores = coverage.NotCovered.Select(store => store.Value).ToArray() };
    }

    static void EnsureListed(ComplianceKeyCoverage coverage)
    {
        if (coverage.ListingFailed)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Compliance event store coverage could not be established."));
        }
    }

    static void EnsureCovered(ICollection<string> notCovered)
    {
        if (notCovered.Count > 0)
        {
            throw new RpcException(
                new Status(StatusCode.PermissionDenied, "Compliance operation did not cover every event store."),
                new Metadata { { "not-covered-event-stores-bin", JsonSerializer.SerializeToUtf8Bytes(notCovered) } });
        }
    }

    Task<JsonObject> ReleaseContent(ReleaseRequest request, JsonSchema schema, JsonObject json) =>
        request.Strict
            ? schemaMetadataManager.ReleaseStrict((EventStoreName)request.EventStore, (EventStoreNamespaceName)request.Namespace, schema, request.Subject, json)
            : schemaMetadataManager.Release((EventStoreName)request.EventStore, (EventStoreNamespaceName)request.Namespace, schema, request.Subject, json);

    ReleaseResponse StrictFailure(Exception exception, string? propertyPath = null)
    {
        logger.FailedToReleaseStrictly(exception.GetType().Name, propertyPath ?? string.Empty);
        return new ReleaseResponse
        {
            HasError = true,
            Error = "Protected content could not be released.",
            FailedProperties = propertyPath is null ? [] : [propertyPath],
            Strict = true
        };
    }

    IPIIManager GetPIIManager(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        grainFactory.GetGrain<IPIIManager>(Guid.Empty, new PIIManagerKey(eventStore, @namespace));
}
