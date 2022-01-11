// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DetailsList, IDetailsListProps, IDetailsListStyles, Stack } from '@fluentui/react';
import { useEffect } from 'react';
import { uniqueIdentifier } from '../uniqueIdentifier';
import { Pagination, IPaginationProps } from '@fluentui/react-experiments';


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

export interface IScrollableDetailsListProps extends IDetailsListProps {
    paging?: IPaginationProps;
}


export const ScrollableDetailsList = (props: IScrollableDetailsListProps) => {

    const identifier = uniqueIdentifier('scrollableDetailsList');
    useEffect(() => {
        const detailsList = document.querySelector(`.ms-DetailsList.${identifier}`);
        if (detailsList) {
            detailsList.parentElement!.style!.height = '100%';
        }
    }, []);

    const paging = props.paging || {
        pageCount: 0,
        itemsPerPage: 0
    };

    const detailsList = <DetailsList {...props} className={identifier} styles={gridStyles} />;


    if (paging.itemsPerPage && paging.itemsPerPage > 0) {
        return (
            <Stack style={{ height: '100%' }}>
                <Stack.Item grow={1} >
                    {detailsList}
                </Stack.Item>
                <Stack.Item>
                    {/* https://codepen.io/micahgodbolt/pen/jXNLvB */}
                    <Pagination
                        format="buttons"
                        {...paging} />
                </Stack.Item>
            </Stack>
        );
    } else {
        return detailsList;
    }
};
