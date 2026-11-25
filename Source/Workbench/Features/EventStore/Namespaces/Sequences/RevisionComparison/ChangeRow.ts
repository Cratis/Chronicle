// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeKind } from './ChangeKind';

/** One difference between two retained revisions. */
export interface ChangeRow {
    /** The RFC 6901 JSON Pointer of the difference; an empty string is the root. */
    path: string;
    /** How the path differs. */
    kind: ChangeKind;
    /** The bounded text of the left value; undefined when the path is absent on the left. */
    before?: string;
    /** The bounded text of the right value; undefined when the path is absent on the right. */
    after?: string;
}
