// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Paginator as PrimePaginator, usePaginatorContext } from 'primereact/paginator';
import type { UsePaginatorChangeEvent } from '@primereact/headless/paginator';

/**
 * Renders one button per page slot the paginator exposes, letting it own the
 * sibling/edge/ellipsis windowing rather than emitting a button per page.
 */
const PageButtons = () => {
    const paginator = usePaginatorContext();

    return (
        <>
            {paginator?.pages?.map((item, index) => (
                item.type === 'page' && item.value !== undefined
                    ? <PrimePaginator.Page key={`page-${item.value}`} value={item.value} />
                    : <PrimePaginator.Ellipsis key={`ellipsis-${index}`} />
            ))}
        </>
    );
};

/**
 * Props for {@link Paginator}.
 */
export interface PaginatorProps {
    /** The current page, zero-based — matching Arc's `result.paging.page`. */
    page: number;
    /** Number of rows on a page. */
    pageSize: number;
    /** Total number of rows across all pages. */
    totalRecords: number;
    /** Called with the new zero-based page. */
    onPageChange: (page: number) => void;
    /** Applied to the paginator root. */
    className?: string;
}

/**
 * A pager over PrimeReact 11's compositional `Paginator` parts.
 *
 * Besides the composition, v11 changed the counting: it takes a **one-based**
 * `page` plus `total`/`itemsPerPage`, where v10 took a zero-based `first` row
 * offset. Arc's paging is zero-based, so this wrapper owns that conversion in
 * one place instead of at each call site.
 */
export const Paginator = ({ page, pageSize, totalRecords, onPageChange, className }: PaginatorProps) => (
    <PrimePaginator.Root
        page={page + 1}
        total={totalRecords}
        itemsPerPage={pageSize}
        onPageChange={(event: UsePaginatorChangeEvent) => onPageChange(event.value - 1)}
        className={className}>
        <PrimePaginator.Content>
            <PrimePaginator.First />
            <PrimePaginator.Prev />
            <PrimePaginator.Pages>
                <PageButtons />
            </PrimePaginator.Pages>
            <PrimePaginator.Next />
            <PrimePaginator.Last />
        </PrimePaginator.Content>
    </PrimePaginator.Root>
);
