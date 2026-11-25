// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Documented UI safety budgets for comparing retained revisions. They bound the work and the
 * output of the comparison in the browser; they are not backend domain limits.
 */
export const ComparisonBudget = {
    /** UTF-16 code units accepted per side. */
    maxCharacters: 262_144,
    /** Deepest value nesting accepted, counting the root as 0. */
    maxDepth: 32,
    /** Values and properties visited in total across both sides. */
    maxVisited: 5_000,
    /** Change rows produced. */
    maxRows: 250,
    /** Characters shown for one scalar value. */
    maxPreview: 256,
    /** Characters shown for one path. */
    maxPath: 512,
    /** Retained revision entries a comparison accepts. */
    maxRevisions: 200,
    /** Characters accepted for one piece of revision identity text. */
    maxIdentity: 128
} as const;
