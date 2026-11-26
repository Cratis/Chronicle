// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, type ComponentType } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { Column, DataTableCore, DataTableFilterMatchMode } from '@cratis/components/DataTables';
import { originColumn } from '../eventTypeColumns';

const rows = ['store-a', 'store-a-remote', 'store-b'].map(originText => ({ id: originText, originText }));
const options = rows.map(row => ({ label: row.originText, value: row.originText }));

const render = (filterValue: string | null) => renderToStaticMarkup(
    createElement(DataTableCore as unknown as ComponentType<Record<string, unknown>>, {
        data: rows,
        dataKey: 'id',
        defaultFilters: { originText: { value: filterValue, matchMode: DataTableFilterMatchMode.Equals } }
    }, createElement(Column, { field: 'id', header: 'Id' }), originColumn(options)));

describe('when the origin column is the filtered column', () => {
    const column = originColumn(options);

    it('should not offer the match mode selector', () => (column.props.showFilterMatchModes as boolean).should.equal(false));
    it('should use a data type whose default match mode is equals, so clearing cannot fall back to contains', () =>
        (column.props.dataType as string).should.equal('numeric'));
    it('should offer the origins as filter options', () => (column.props.filterOptions as unknown[]).length.should.equal(3));
});

describe('when filtering by an origin that is a prefix of another origin', () => {
    const markup = render('store-a');

    it('should show the exact match', () => markup.should.contain('>store-a<'));
    it('should not show the longer origin', () => markup.should.not.contain('store-a-remote'));
    it('should not show unrelated origins', () => markup.should.not.contain('store-b'));
});

describe('when the origin filter is cleared', () => {
    const markup = render(null);

    it('should show every origin', () => {
        markup.should.contain('>store-a<');
        markup.should.contain('store-a-remote');
        markup.should.contain('store-b');
    });
});
