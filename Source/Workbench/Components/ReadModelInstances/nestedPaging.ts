// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The number of nested rows shown on a page unless the user picks another size. */
export const defaultNestedPageSize = 50;

/** The page sizes a user can pick for nested rows. */
export const nestedPageSizeOptions: readonly number[] = [10, 25, 50, 100];

/**
 * Calculates the number of pages the rows span; an empty collection still has one page.
 * @param totalItems The number of rows.
 * @param pageSize The number of rows on a page.
 * @returns The number of pages.
 */
export const nestedPageCount = (totalItems: number, pageSize: number): number =>
    Math.max(1, Math.ceil(totalItems / Math.max(1, pageSize)));

/**
 * Keeps a requested page inside the pages that exist, so a shrinking collection never leaves a blank page behind.
 * @param page The requested zero based page.
 * @param totalItems The number of rows.
 * @param pageSize The number of rows on a page.
 * @returns The page to show.
 */
export const clampNestedPage = (page: number, totalItems: number, pageSize: number): number =>
    Math.min(Math.max(0, page), nestedPageCount(totalItems, pageSize) - 1);

/** The paging of a set of rows. */
export interface PagingState {
    page: number;
    pageSize: number;
    totalItems: number;
}

/** The paging the controls show. */
export interface ResolvedPaging extends PagingState {
    pageCount: number;
}

/**
 * Decides which paging the controls show: the caller owned paging of the root rows, or the local paging
 * of the nested rows. Nested paging never describes, or changes, the root paging.
 * @param isNested Whether a nested collection is being browsed.
 * @param root The paging the caller owns.
 * @param nested The local paging of the nested rows.
 * @returns The paging to show.
 */
export const resolvePaging = (isNested: boolean, root: PagingState, nested: PagingState): ResolvedPaging => {
    const shown = isNested ? nested : root;
    return {
        ...shown,
        page: isNested ? clampNestedPage(nested.page, nested.totalItems, nested.pageSize) : root.page,
        pageCount: isNested ? nestedPageCount(nested.totalItems, nested.pageSize) : Math.ceil(root.totalItems / root.pageSize),
    };
};
