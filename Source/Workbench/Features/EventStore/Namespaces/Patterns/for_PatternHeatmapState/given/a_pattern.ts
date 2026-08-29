// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';

export const aPattern = (facets: Record<string, string>, confidence: number): BehaviorPattern =>
    Object.assign(new BehaviorPattern(), {
        id: Object.entries(facets).map(([name, value]) => `${name}=${value}`).join(';'),
        groupingKey: 'user-42',
        facets,
        confidence,
        support: 0.5,
        occurrences: 10n,
        weight: 1,
        specificity: Object.keys(facets).length,
        firstSeen: new Date(0),
        lastSeen: new Date(0),
    });
