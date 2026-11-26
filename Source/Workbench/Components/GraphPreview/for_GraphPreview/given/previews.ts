// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { GraphProjectionPreview } from 'Features/ProjectionEditor';
import { organization } from '../../for_graphViews/given/organization';

/** A preview of the Organization graph: two people, a group and one membership. */
export const organizationPreview = (overrides: Partial<GraphProjectionPreview> = {}): GraphProjectionPreview => ({
    graph: organization,
    nodes: [
        { label: 'Person', key: 'p1', properties: { personId: 'p1', name: 'Ada' } },
        { label: 'Person', key: 'p2', properties: { personId: 'p2', name: 'Grace' } },
        { label: 'Group', key: 'g1', properties: { groupId: 'g1', name: 'Engineering' } }
    ],
    edges: [
        { type: 'MEMBER_OF', sourceLabel: 'Person', sourceKey: 'p1', targetLabel: 'Group', targetKey: 'g1', identity: undefined, properties: { since: '2026-01-06T00:00:00.0000000+00:00' } }
    ],
    rows: [],
    eventsProcessed: 5,
    eventErrors: [],
    viewErrors: [],
    syntaxErrors: [],
    ...overrides
}) as GraphProjectionPreview;
