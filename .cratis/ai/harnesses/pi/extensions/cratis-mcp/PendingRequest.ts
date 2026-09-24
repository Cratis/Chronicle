// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/PendingRequest.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface PendingRequest {
    id: number;
    mutation: boolean;
    resolve(value: unknown): void;
    reject(error: Error): void;
    cleanup(): void;
}
