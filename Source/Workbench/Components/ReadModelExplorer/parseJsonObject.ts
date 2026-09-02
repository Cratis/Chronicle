// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Reads one of the JSON payloads a read model snapshot carries.
 *
 * A snapshot carries its read model instance and each event's content as the JSON text they were stored
 * as rather than as parsed objects - the server has no schema to shape them against at that point, so
 * every client parses them against what it knows. A viewer is not the place for a payload to fail, so
 * unparseable text reads as nothing rather than throwing on render.
 * @param json The payload as JSON text.
 * @returns The parsed object, or an empty object when there is nothing to read.
 */
export const parseJsonObject = (json: string | null | undefined): Record<string, unknown> => {
    if (!json) return {};

    try {
        const parsed = JSON.parse(json);
        return typeof parsed === 'object' && parsed !== null ? parsed as Record<string, unknown> : {};
    } catch {
        return {};
    }
};
