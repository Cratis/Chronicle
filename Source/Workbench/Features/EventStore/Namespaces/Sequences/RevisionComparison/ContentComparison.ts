// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeRow } from './ChangeRow';
import { ComparisonSide } from './ComparisonSide';
import { LimitReason } from './LimitReason';

/**
 * The result of comparing two revision payloads. Anything other than `identical` and `different`
 * means the comparison is not a complete statement about the two payloads.
 */
export type ComparisonOutcome = 'identical' | 'different' | 'limited' | 'unavailable' | 'invalid' | 'unsupportedNumber';

/** The result of comparing two revision payloads. */
export interface ContentComparison {
    outcome: ComparisonOutcome;
    /** The differences found; empty unless the outcome is `different` or `limited`. */
    rows: ChangeRow[];
    /** The side that made the comparison unavailable, invalid, unsupported or limited before any work. */
    side?: ComparisonSide;
    /** The budget that stopped the comparison. */
    limit?: LimitReason;
}
