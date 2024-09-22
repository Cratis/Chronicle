// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { TreeNode } from 'primereact/treenode';
import { TreeTable } from 'primereact/treetable';
import { AppendedEventWithJsonAsContent } from 'Api/EventSequences';
import { IDetailsComponentProps } from 'Components';

const addContent = (node: TreeNode, current: any, currentLevel: string) => {
    for (const key in current) {
        if (current.hasOwnProperty(key)) {
            const value = current[key];
            const keyNode: TreeNode = {
                key: `${currentLevel}-${key}`,
                data: {
                    name: key,
                    value: value
                },
                children: []
            };
            node.children?.push(keyNode);
            if (typeof value === 'object') {
                addContent(keyNode, value, `${currentLevel}-${key}`);
            }
        }
    }
};

const buildContextNode = (event: AppendedEventWithJsonAsContent) => {
    const contextNode: TreeNode = {
        key: '0',
        data: {
            name: 'Context'
        },
        children: [
            {
                key: '0-0',
                data: {
                    name: 'occurred',
                    value: event.context.occurred.toLocaleString()
                }
            },
            {
                key: '0-1',
                data: {
                    name: 'eventSourceId',
                    value: event.context.eventSourceId
                }
            },
            {
                key: '0-2',
                data: {
                    name: 'correlationId',
                    value: event.context.correlationId.toString()
                }
            },
            {
                key: '0-3',
                data: {
                    name: 'caused by',
                    value: event.context.causedBy.name
                }
            }
        ],
    };

    const causationNode: TreeNode = {
        key: '0-4',
        data: {
            name: 'Causation'
        },
        children: event.context.causation.map((c, index) => {
            return {
                key: `0-4-${index}`,
                data: {
                    name: `causation ${index}`,
                    value: c.toString()
                }
            };
        })
    };
    contextNode.children!.push(causationNode);
    return contextNode;
};

export const EventDetails = (props: IDetailsComponentProps<AppendedEventWithJsonAsContent>) => {
    const contextNodes: TreeNode[] = [];

    contextNodes.push(buildContextNode(props.item));

    const contentNode: TreeNode = {
        key: '1',
        data: {
            name: 'Content'
        },
        children: [],
    };

    contextNodes.push(contentNode);

    const current = props.item.content;
    const currentLevel = '1';
    addContent(contentNode, current, currentLevel);

    return (
        <TreeTable value={contextNodes} showGridlines={false} >
            <Column field='name' header='Property' expander />
            <Column field='value' header='Value' />
        </TreeTable>
    );
};
