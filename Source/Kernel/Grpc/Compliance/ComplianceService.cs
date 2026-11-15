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
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        var coverage = await authorization.KeyLifecycle("delete-key", request.EventStore, request.Namespace, request.Identifier, ComplianceKeyOperation.Delete, context);
        EnsureRequestedStoreAllowed(coverage);
        EnsureListed(coverage);
        EnsureComplete(coverage.NotCovered.Select(store => store.Value).ToArray());
        if (coverage.Allowed.Count > 0)
        {
            await DeleteWithRequest(request, coverage.Allowed.ToArray());
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewEncryptionKey(AllowNewEncryptionKeyRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        var coverage = await authorization.KeyLifecycle("allow-new-key", request.EventStore, request.Namespace, request.Identifier, ComplianceKeyOperation.AllowNew, context);
        EnsureRequestedStoreAllowed(coverage);
        EnsureListed(coverage);
        EnsureComplete(coverage.NotCovered.Select(store => store.Value).ToArray());
        if (coverage.Allowed.Count > 0)
        {
            await GetPIIManager(request.EventStore, request.Namespace).AllowNewEncryptionKeyIn(request.Identifier, coverage.Allowed.ToArray());
        }
    }

    /// <inheritdoc/>
    public async Task<DeleteEncryptionKeyResponse> DeleteEncryptionKeyWithCoverage(DeleteEncryptionKeyRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        var coverage = await authorization.KeyLifecycle("delete-key", request.EventStore, request.Namespace, request.Identifier, ComplianceKeyOperation.Delete, context);
        EnsureRequestedStoreAllowed(coverage);
        if (coverage.Allowed.Count > 0)
        {
            await DeleteWithRequest(request, coverage.Allowed.ToArray());
        }

        EnsureListed(coverage);
        return new() { NotCoveredEventStores = coverage.NotCovered.Select(store => store.Value).ToArray() };
    }

    /// <inheritdoc/>
    public async Task<AllowNewEncryptionKeyResponse> AllowNewEncryptionKeyWithCoverage(AllowNewEncryptionKeyRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.Namespace));
        var coverage = await authorization.KeyLifecycle("allow-new-key", request.EventStore, request.Namespace, request.Identifier, ComplianceKeyOperation.AllowNew, context);
        EnsureRequestedStoreAllowed(coverage);
        if (coverage.Allowed.Count > 0)
        {
            await GetPIIManager(request.EventStore, request.Namespace).AllowNewEncryptionKeyIn(request.Identifier, coverage.Allowed.ToArray());
        }

        EnsureListed(coverage);
        return new() { NotCoveredEventStores = coverage.NotCovered.Select(store => store.Value).ToArray() };
    }

    static void EnsureComplete(string[] notCovered)
    {
        if (notCovered.Length > 0)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, $"Compliance operation did not cover event stores: {string.Join(", ", notCovered)}."));
        }
    }

    static void EnsureRequestedStoreAllowed(ComplianceKeyCoverage coverage)
    {
        if (coverage.RequestedDenied)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Compliance operation is not permitted."));
        }
    }

    static void EnsureListed(ComplianceKeyCoverage coverage)
    {
        if (coverage.ListingFailed)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Compliance event store coverage could not be established."));
        }
    }

    Task DeleteWithRequest(DeleteEncryptionKeyRequest request, EventStoreName[] eventStores)
    {
        var manager = GetPIIManager(request.EventStore, request.Namespace);
        if (request.RequestId is null) return manager.DeleteEncryptionKeyIn(request.Identifier, eventStores);
        try
        {
            Storage.Compliance.EncryptionKeyErasureRequestIdentity.Validate(request.RequestId);
        }
        catch (ArgumentException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "An erasure request id must contain between 1 and 128 characters and cannot be blank."));
        }

        return manager.DeleteEncryptionKeyInWithRequest(request.Identifier, eventStores, request.RequestId);
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
