// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { GraphPreviewSchema } from 'Features/ProjectionEditor';

/** The Organization graph: people, groups, MEMBER_OF between them and groups nested in groups. */
export const organization: GraphPreviewSchema = {
    name: 'Organization',
    nodeTypes: [
        { label: 'Person', keyProperty: 'personId', properties: ['personId', 'name'] },
        { label: 'Group', keyProperty: 'groupId', properties: ['groupId', 'name'] }
    ],
    edgeTypes: [
        { type: 'MEMBER_OF', sourceLabel: 'Person', targetLabel: 'Group', properties: ['since'] },
        { type: 'PART_OF', sourceLabel: 'Group', targetLabel: 'Group', properties: [] }
    ]
};
