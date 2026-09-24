// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/ConnectionFailure.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A stopped connection is never restarted or a request retried implicitly. */
export class ConnectionFailure extends Error {
    constructor(message: string, readonly outcomeUnknown = false) {
        super(outcomeUnknown
            ? `${message} Source mutation outcome is unknown. Do not retry apply/recovery. Inspect workspace-state in a new explicitly opened session before deciding on recovery.`
            : message);
    }
}
