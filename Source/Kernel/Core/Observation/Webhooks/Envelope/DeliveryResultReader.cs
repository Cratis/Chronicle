// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Reads the bounded delivery-v1 receiver result without kernel JSON converters.
/// </summary>
internal static class DeliveryResultReader
{
    /// <summary>
    /// The largest receiver result body permitted, in bytes.
    /// </summary>
    internal const int MaximumResultBytes = 1024 * 1024;

    /// <summary>
    /// Reads and classifies the receiver's acknowledgement of the batch.
    /// </summary>
    /// <param name="response">The successful HTTP response.</param>
    /// <param name="delivery">The delivery inputs.</param>
    /// <param name="batch">The ordered batch of events.</param>
    /// <param name="cancellationToken">Cancellation for reading the body.</param>
    /// <returns>The acknowledgement or a carried delivery error.</returns>
    internal static async Task<WebhookDeliveryResult> Read(HttpResponseMessage response, HttpDeliveryRequest delivery, AppendedEvent[] batch, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NoContent) return FullAcknowledgement(batch);
        var body = await ReadBody(response.Content, cancellationToken);
        if (body.Length > MaximumResultBytes) return Malformed(delivery, "the result exceeds the maximum size");
        if (string.IsNullOrWhiteSpace(Encoding.UTF8.GetString(body.Span))) return FullAcknowledgement(batch);
        return Parse(body, delivery, batch);
    }

    static async Task<ReadOnlyMemory<byte>> ReadBody(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaximumResultBytes + 1];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
            if (read == 0) break;
            length += read;
        }
        return buffer.AsMemory(0, length);
    }

    static WebhookDeliveryResult Parse(ReadOnlyMemory<byte> body, HttpDeliveryRequest delivery, AppendedEvent[] batch)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var results = ReadProperty(document.RootElement, "results");
            if (results.ValueKind != JsonValueKind.Array)
            {
                return Malformed(delivery, "the result must contain a results array");
            }
            if (results.GetArrayLength() != 1) return Malformed(delivery, "the results array must have exactly one entry");
            return ParseEntry(results[0], delivery, batch);
        }
        catch (JsonException)
        {
            return Malformed(delivery, "the result is not valid JSON");
        }
    }

    static WebhookDeliveryResult ParseEntry(JsonElement entry, HttpDeliveryRequest delivery, AppendedEvent[] batch)
    {
        if (entry.ValueKind != JsonValueKind.Object) return Malformed(delivery, "the result entry is not an object");
        if (ReadString(entry, "partition") != delivery.Partition.ToString()) return Malformed(delivery, "the result names another partition");
        var success = ReadState(entry);
        if (success is null) return Malformed(delivery, "the result state is not recognized");
        if (!TryReadCheckpoint(entry, out var checkpoint)) return Malformed(delivery, "the checkpoint is not an unsigned sequence number");
        var result = ReadSafeResult(entry, success.Value, checkpoint, delivery.Target.Authorization.IsT4);
        return Classify(result, delivery, batch);
    }

    static bool TryReadCheckpoint(JsonElement entry, out ulong? checkpoint)
    {
        checkpoint = null;
        var value = ReadProperty(entry, "lastSuccessfulObservation");
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return true;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetUInt64(out var number)) return false;
        checkpoint = number;
        return true;
    }

    static WebhookDeliveryResult Classify(ReceiverResult result, HttpDeliveryRequest delivery, AppendedEvent[] batch)
    {
        if (AcknowledgesNothing(result.Checkpoint, FirstEvent(batch)))
        {
            return result.Success
                ? Malformed(delivery, "a success requires a checkpoint in the batch")
                : NotFullyAcknowledged(result, delivery, EventSequenceNumber.Unavailable);
        }
        EventSequenceNumber checkpoint = result.Checkpoint!.Value;
        if (!batch.Any(@event => @event.Context.SequenceNumber == checkpoint))
        {
            return Malformed(delivery, "the checkpoint is not an event of the batch");
        }
        if (checkpoint != LastEvent(batch)) return NotFullyAcknowledged(result, delivery, checkpoint);
        return result.Success ? FullAcknowledgement(batch) : Malformed(delivery, "a failure cannot acknowledge the last event of the batch");
    }

    static bool AcknowledgesNothing(ulong? checkpoint, EventSequenceNumber first) => checkpoint is null or ulong.MaxValue || checkpoint < first.Value;

    static EventSequenceNumber FirstEvent(AppendedEvent[] batch) => batch.FirstOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable;

    static EventSequenceNumber LastEvent(AppendedEvent[] batch) => batch.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable;

    static JsonElement ReadProperty(JsonElement entry, string name) =>
        entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty(name, out var value) ? value : default;

    static string? ReadString(JsonElement entry, string name)
    {
        var value = ReadProperty(entry, name);
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    static bool? ReadState(JsonElement entry) => ReadString(entry, "state")?.ToLowerInvariant() switch
    {
        "success" => true,
        "failed" => false,
        _ => null
    };

    static ReceiverResult ReadSafeResult(JsonElement entry, bool success, ulong? checkpoint, bool managedIdentity)
    {
        var messages = managedIdentity ? [] : ReadMessages(entry);
        var stackTrace = managedIdentity ? string.Empty : ReadString(entry, "exceptionStackTrace") ?? string.Empty;
        return new(success, checkpoint, messages, stackTrace);
    }

    static string[] ReadMessages(JsonElement entry)
    {
        var messages = ReadProperty(entry, "exceptionMessages");
        return messages.ValueKind == JsonValueKind.Array
            ? messages.EnumerateArray().Where(message => message.ValueKind == JsonValueKind.String).Select(message => message.GetString()!).ToArray()
            : [];
    }

    static WebhookDeliveryResult FullAcknowledgement(AppendedEvent[] batch) => new(LastEvent(batch));

    static WebhookDeliveryResult Malformed(HttpDeliveryRequest delivery, string reason) => new(EventSequenceNumber.Unavailable, new DeliveryResultMalformed(delivery.Target.Url, reason));

    static WebhookDeliveryResult NotFullyAcknowledged(ReceiverResult result, HttpDeliveryRequest delivery, EventSequenceNumber checkpoint) =>
        new(checkpoint, new DeliveryNotFullyAcknowledged(delivery.Target.Url, checkpoint, result.Messages, result.StackTrace));

    /// <summary>
    /// A validated receiver entry with only safe diagnostic text retained.
    /// </summary>
    /// <param name="Success">Whether the receiver reported success.</param>
    /// <param name="Checkpoint">The optional unsigned checkpoint.</param>
    /// <param name="Messages">The safe receiver messages.</param>
    /// <param name="StackTrace">The safe receiver stack trace.</param>
    sealed record ReceiverResult(bool Success, ulong? Checkpoint, string[] Messages, string StackTrace);
}
