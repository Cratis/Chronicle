// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { TreeNode } from 'primereact/treenode';
import { TreeTable } from 'primereact/treetable';
import { EventTypeWithSchemas } from 'Api/EventTypes';
import { IDetailsComponentProps } from 'Components';

const formatType = (type: string) => {
    switch (type) {
        case 'date-time-offset': return 'DateTimeOffset';
        case 'date-time': return 'DateTime';
        case 'date-only': return 'DateOnly';
        case 'time-only': return 'TimeOnly';
    }

    return type;
};

export const TypeDetails = (props: IDetailsComponentProps<EventTypeWithSchemas>) => {
    const propertyNodes: TreeNode[] = [];

    const properties = props.item.schemas[0].properties;
    if (properties) {
        let index = 0;

        for (const propertyName in properties) {
            const property = properties[propertyName];
            let type = property.format;
            if (!property.format) {
                type = property.type;
                if (Array.isArray(type)) {
                    type = type[type.length - 1];
                }
            }

            propertyNodes.push({
                key: index,
                data: {
                    name: propertyName,
                    value: formatType(type)
                }
            });
            index++;
        }
    }

    return (
        <TreeTable value={propertyNodes} showGridlines={false}>
            <Column field='name' header='Property' expander />
            <Column field='value' header='Value' />
        </TreeTable>
    );
};
