// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    IconButton,
    Stack,
    Pivot,
    PivotItem,
    IPivotItemProps
} from '@fluentui/react';

function pivotItemHeaderRenderer(
    link?: IPivotItemProps,
    defaultRenderer?: (link?: IPivotItemProps) => JSX.Element | null,
): JSX.Element | null {
    if (!link || !defaultRenderer) {
        return null;
    }

    return (
        <span style={{ flex: '0 1 100%' }}>
            <IconButton iconProps={{ iconName: 'StatusCircleErrorX' }} title="Close query" onClick={() => alert('hello world')} />
            {defaultRenderer({ ...link, itemIcon: undefined })}
        </span>
    );
}


export const Projections = () => {
    return (
        <>
            <Stack>
                <Stack.Item disableShrink>
                    <Stack horizontal style={{ textAlign: 'center' }}>
                        <Pivot linkFormat="links">
                            <PivotItem key="5c5af4ee-282a-456c-a53d-e3dee158a3be" headerText="Untitled" onRenderItemLink={pivotItemHeaderRenderer} />
                            <PivotItem key="b7a5f0a3-82d3-4170-a1e7-36034d763008" headerText="Good old query" onRenderItemLink={pivotItemHeaderRenderer} />
                        </Pivot>
                        <IconButton iconProps={{ iconName: 'Add' }} title="Add query" />
                    </Stack>
                </Stack.Item>
            </Stack>
        </>
    );
}
