// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { Dropdown } from '@cratis/components/Dropdown';
import { TablePaginator } from '@cratis/components/DataTables';
import strings from 'Strings';
import { nestedPageSizeOptions, type ResolvedPaging } from './nestedPaging';

interface InstancePagerProps {
    paging: ResolvedPaging;
    onPageChange: (page: number) => void;
    onPageSizeChange: (pageSize: number) => void;
}

/** The paging controls of the rows being shown, whichever set of rows that is. */
export const InstancePager = ({ paging, onPageChange, onPageSizeChange }: InstancePagerProps) => {
    const pageSizeOptions = useMemo(() => nestedPageSizeOptions.map(size => ({ label: String(size), value: size })), []);

    return (
        <div
            style={{
                borderTop: '1px solid var(--surface-border)',
                flexShrink: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: '0.5rem'
            }}>
            <TablePaginator
                page={paging.page}
                pageCount={paging.pageCount}
                pageSize={paging.pageSize}
                totalItems={paging.totalItems}
                onPageChange={onPageChange}
            />
            <Dropdown<number>
                value={paging.pageSize}
                options={pageSizeOptions}
                optionLabel='label'
                optionValue='value'
                aria-label={strings.eventStore.namespaces.readModels.labels.rowsPerPage}
                onChange={onPageSizeChange}
            />
        </div>
    );
};
