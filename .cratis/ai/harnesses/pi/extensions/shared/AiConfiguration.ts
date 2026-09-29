// cratis-ai-managed: harnesses/pi/extensions/shared/AiConfiguration.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The parts of a repository's `.cratis/ai.json` that rule and skill selection read. */
export interface AiConfiguration {
    profiles?: string[];
    languages?: string[];
}
