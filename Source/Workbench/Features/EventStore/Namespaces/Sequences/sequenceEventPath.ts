// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function isInspectableSequenceNumber(value: number): boolean {
    return Number.isInteger(value) && value >= 0 && value <= 2147483647;
}

export function sequenceIdFromSearch(search: URLSearchParams): string | undefined {
    const values = search.getAll('eventSequenceId');
    return values.length === 1 && values[0].length > 0 ? values[0] : undefined;
}

export function sequenceEventPath(eventStore: string, namespace: string, eventSequenceId: string, sequenceNumber: number, failedPartitionId?: string): string | undefined {
    if (!eventStore || !namespace || !eventSequenceId || !isInspectableSequenceNumber(sequenceNumber)) return undefined;
    const search = new URLSearchParams();
    search.set('eventSequenceId', eventSequenceId);
    // URLSearchParams converts to USVString. Reject identities it would silently change.
    if (search.get('eventSequenceId') !== eventSequenceId) return undefined;
    if (failedPartitionId) {
        search.set('failedPartitionId', failedPartitionId);
        if (search.get('failedPartitionId') !== failedPartitionId) return undefined;
    }
    return `/event-store/${encodeURIComponent(eventStore)}/${encodeURIComponent(namespace)}/sequences/event/${sequenceNumber}?${search}`;
}

export function failedPartitionsPath(eventStore: string, namespace: string, failedPartitionId?: string): string {
    const search = new URLSearchParams();
    if (failedPartitionId) search.set('failedPartitionId', failedPartitionId);
    return `/event-store/${encodeURIComponent(eventStore)}/${encodeURIComponent(namespace)}/failed-partitions${search.size ? `?${search}` : ''}`;
}
