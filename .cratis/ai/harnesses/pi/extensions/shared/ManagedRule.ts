// cratis-ai-managed: harnesses/pi/extensions/shared/ManagedRule.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface ManagedRule {
    /** Path relative to the rules root, e.g. `code-quality-csharp.md` or `project/running-the-local-stack.md`. */
    name: string;
    /** Full file content, frontmatter included, exactly as the other harnesses receive it. */
    content: string;
    /** `application`, `framework`, or undefined when the rule is not profile-specific. */
    profile?: string;
    /** Globs from `applyTo` and `paths`. Empty means the rule applies to every file. */
    globs: string[];
}
