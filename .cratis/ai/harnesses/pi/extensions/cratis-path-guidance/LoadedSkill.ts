// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/LoadedSkill.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The part of a Pi skill (`event.systemPromptOptions.skills`) this extension reads. */
export interface LoadedSkill {
    name: string;
    filePath: string;
    baseDir?: string;
}
