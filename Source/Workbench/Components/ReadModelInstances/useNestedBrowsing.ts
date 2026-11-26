// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useState } from 'react';
import type { Json } from 'Features';
import type { ReadModelInstance } from 'Features/ReadModelExplorer';
import { getNestedRows } from './nestedRows';
import { defaultNestedPageSize, resolvePaging, type PagingState } from './nestedPaging';

type Row = { [k: string]: Json };

const isRow = (value: Json | undefined): value is Row => value !== null && typeof value === 'object' && !Array.isArray(value);

/**
 * Follows the path the user browsed into, and pages the rows found there on its own. The paging the caller
 * owns only ever describes the root rows; browsing, paging and sizing nested rows never touch it.
 * @param instances The root instances currently loaded.
 * @param root The paging the caller owns for the root rows.
 */
export const useNestedBrowsing = (instances: ReadModelInstance[], root: PagingState) => {
    const [navigationPath, setNavigationPath] = useState<string[]>([]);
    const [nestedPage, setNestedPage] = useState(0);
    const [nestedPageSize, setNestedPageSize] = useState(defaultNestedPageSize);
    const isNested = navigationPath.length > 0;

    // Another root page, page size or set of instances is another source for the nested rows.
    useEffect(() => { setNestedPage(0); }, [instances, root.page, root.pageSize]);

    const documents = useMemo(() => (instances ?? []).map(item => item.instance as Json), [instances]);
    const nestedTotal = useMemo(
        () => isNested ? getNestedRows(documents, navigationPath, 0, 0).total : 0,
        [documents, navigationPath, isNested]);
    const paging = resolvePaging(isNested, root, { page: nestedPage, pageSize: nestedPageSize, totalItems: nestedTotal });
    const nested = useMemo(
        () => isNested ? getNestedRows(documents, navigationPath, paging.page * nestedPageSize, nestedPageSize) : null,
        [documents, navigationPath, isNested, paging.page, nestedPageSize]);

    const rows = useMemo<Row[]>(() => nested ? nested.rows : documents.filter(isRow), [nested, documents]);
    const columnKeys = useMemo<string[]>(() => {
        if (nested) return nested.columns;
        const first = documents[0];
        return isRow(first) ? Object.keys(first).filter(key => !key.startsWith('__')) : [];
    }, [nested, documents]);

    const navigateInto = useCallback((key: string) => {
        setNavigationPath(path => [...path, key]);
        setNestedPage(0);
    }, []);

    const navigateToBreadcrumb = useCallback((index: number) => {
        setNavigationPath(path => index === 0 ? [] : path.slice(0, index));
        setNestedPage(0);
    }, []);

    const returnToRoot = useCallback(() => {
        setNavigationPath([]);
        setNestedPage(0);
    }, []);

    const changeNestedPageSize = useCallback((size: number) => {
        setNestedPageSize(size);
        setNestedPage(0);
    }, []);

    return { navigationPath, isNested, rows, columnKeys, paging, navigateInto, navigateToBreadcrumb, returnToRoot, setNestedPage, changeNestedPageSize };
};
