// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/SkillMatch.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { SkillTrigger } from './SkillTrigger.ts';

/** A skill whose trigger matched a path, with the glob that matched. */
export interface SkillMatch {
    skill: SkillTrigger;
    glob: string;
}
