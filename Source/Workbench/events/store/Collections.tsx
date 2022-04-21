// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DetailsList, IColumn, IDetailsListStyles } from '@fluentui/react';
import { Collections as ProjectionCollections } from 'API/events/store/projections/Collections';

const gridStyles: Partial<IDetailsListStyles> = {
    root: {
        height: '100%',
        overflowX: 'scroll',
        selectors: {
            '& [role=grid]': {
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'start'
            },
        },
    },
    headerWrapper: {
        flex: '0 0 auto',
    },
    contentWrapper: {
        flex: '1 1 auto',
        overflowX: 'hidden',
        overflowY: 'auto',
        height: '300px'
    },
};

const columns: IColumn[] = [

    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200,
        isResizable: true
    },
    {
        key: 'documentCount',
        name: 'Document Count',
        fieldName: 'documentCount',
        minWidth: 200,
        isResizable: true
    }
];

export interface CollectionsProps {
    microserviceId: string;
    projectionId: string;
}

export const Collections = (props: CollectionsProps) => {
    const [collections] = ProjectionCollections.use({ microserviceId: props.microserviceId, projectionId: props.projectionId });

    return (
        <DetailsList
            styles={gridStyles}
            columns={columns}
            items={collections.data}
            />
    );
};
