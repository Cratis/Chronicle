// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import { fn } from 'storybook/test';
import { Column } from '@cratis/components/DataTables';
import { Tag } from '@cratis/components/Display';
import { DataTable, type DataTableProps } from './DataTable';

interface Observer {
    id: string;
    handled: number;
    state: 'Active' | 'Failed' | 'Paused';
    silo: string;
    detail: { partition: string };
}

const observers: Observer[] = [
    { id: 'orders-projection', handled: 12043, state: 'Active', silo: 'silo-1', detail: { partition: 'p-01' } },
    { id: 'invoices-reducer', handled: 981, state: 'Failed', silo: 'silo-1', detail: { partition: 'p-02' } },
    { id: 'shipments-reactor', handled: 45210, state: 'Active', silo: 'silo-2', detail: { partition: 'p-03' } },
    { id: 'audit-projection', handled: 0, state: 'Paused', silo: 'silo-2', detail: { partition: 'p-04' } }
];

const stateBody = (row: Observer) => (
    <Tag
        value={row.state}
        severity={row.state === 'Active' ? 'success' : row.state === 'Failed' ? 'danger' : 'warn'} />
);

/**
 * A concrete instantiation of the generic table, so Storybook can infer the args
 * type — `typeof DataTable` alone widens `TData` to `object`.
 */
const ObserverTable = (props: DataTableProps<Observer>) => <DataTable<Observer> {...props} />;

const meta = {
    title: 'Components/DataTable',
    component: ObserverTable,
    parameters: {
        layout: 'padded',
        docs: {
            description: {
                component:
                    'A declarative table over a plain array, rebuilt on PrimeReact 11\'s compositional `DataTable` ' +
                    'primitives. It consumes the same `Column` marker as `DataPage.Columns`, so a table over an ' +
                    'array in hand and a table over an Arc query are authored identically.'
            }
        }
    },
    tags: ['autodocs']
} satisfies Meta<typeof ObserverTable>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The default table — sortable columns, a custom cell body, and a nested `detail.partition` field. */
export const Playground: Story = {
    args: {
        value: observers,
        dataKey: 'id',
        emptyMessage: 'No observers.'
    },
    render: args => (
        <DataTable<Observer> {...args}>
            <Column<Observer> field='id' header='Observer' sortable />
            <Column<Observer> field='handled' header='Handled' sortable />
            <Column<Observer> field='state' header='State' body={stateBody} sortable />
            <Column<Observer> field='detail.partition' header='Partition' />
        </DataTable>
    )
};

/** With nothing to show, the empty message spans the full width of the table. */
export const Empty: Story = {
    args: { value: [], dataKey: 'id', emptyMessage: 'No observers.' },
    render: args => (
        <DataTable<Observer> {...args}>
            <Column<Observer> field='id' header='Observer' />
            <Column<Observer> field='state' header='State' />
        </DataTable>
    )
};

/** A global search box plus per-column filter menus, both applied client-side to the loaded rows. */
export const Filtering: Story = {
    args: {
        value: observers,
        dataKey: 'id',
        emptyMessage: 'No observers.',
        globalFilterFields: ['id', 'silo']
    },
    render: args => (
        <DataTable<Observer> {...args}>
            <Column<Observer> field='id' header='Observer' sortable filter />
            <Column<Observer> field='handled' header='Handled' sortable filter dataType='numeric' />
            <Column<Observer> field='state' header='State' body={stateBody} filter />
        </DataTable>
    )
};

/** Consecutive rows sharing a `groupField` value get a subheader row. */
export const Grouped: Story = {
    args: {
        value: observers,
        dataKey: 'id',
        emptyMessage: 'No observers.',
        groupField: 'silo',
        groupHeaderTemplate: (row: Observer) => <strong>Silo: {row.silo}</strong>
    },
    render: args => (
        <DataTable<Observer> {...args}>
            <Column<Observer> field='id' header='Observer' />
            <Column<Observer> field='handled' header='Handled' />
            <Column<Observer> field='state' header='State' body={stateBody} />
        </DataTable>
    )
};

const SelectionDemo = () => {
    const [selected, setSelected] = useState<Observer | null>(observers[0]);

    return (
        <>
            <DataTable<Observer>
                value={observers}
                dataKey='id'
                emptyMessage='No observers.'
                selectionMode='single'
                selection={selected}
                onSelectionChange={event => setSelected(event.value)}>
                <Column<Observer> field='id' header='Observer' sortable />
                <Column<Observer> field='state' header='State' body={stateBody} />
            </DataTable>
            <p>Selected: {selected?.id ?? 'none'}</p>
        </>
    );
};

/** Single-row selection, translated from PrimeReact 11's key-based model back to the row object. */
export const Interactive: Story = {
    args: { value: observers, emptyMessage: 'No observers.', onSelectionChange: fn() },
    render: () => <SelectionDemo />
};
