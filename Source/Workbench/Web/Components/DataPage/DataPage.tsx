// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useMemo } from 'react';
import { Page } from '../Common/Page';
import React from 'react';
import { MenuItem as PrimeMenuItem } from 'primereact/menuitem';
import { Menubar } from 'primereact/menubar';
import { IObservableQueryFor, IQueryFor, QueryFor } from '@cratis/arc/queries';
import { DataTableForObservableQuery } from '../DataTables/DataTableForObservableQuery';
import { DataTableFilterMeta, DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { DataTableForQuery } from '../DataTables/DataTableForQuery';
import { Allotment } from 'allotment';
import { Constructor } from '@cratis/fundamentals';

/* eslint-disable @typescript-eslint/no-explicit-any */

export interface MenuItemProps extends PrimeMenuItem {
    disableOnUnselected?: boolean;
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export const MenuItem = (_: MenuItemProps) => {
    return null;
};

export interface MenuItemsProps {
    children: ReactNode;
}

export interface ColumnProps {
    children: ReactNode;
}

export const MenuItems = ({ children }: MenuItemsProps) => {
    const context = React.useContext(DataPageContext);

    const isDisabled = useMemo(() => {
        return !context.selectedItem;
    }, [context.selectedItem]);

    const items = useMemo(() => {
        const menuItems: PrimeMenuItem[] = [];
        React.Children.forEach(children, (child) => {
            if (React.isValidElement<MenuItemProps>(child) && child.type == MenuItem) {
                const Icon = child.props.icon;
                const menuItem = { ...child.props };
                menuItem.icon = <Icon className='mr-2' />;
                menuItem.disabled = isDisabled && child.props.disableOnUnselected;
                menuItems.push(menuItem);
            }
        });

        return menuItems;
    }, [children, context.selectedItem]);

    return (
        <div className="px-4 py-2">
            <Menubar aria-label="Actions" model={items} />
        </div>);
};

export const Columns = ({ children }: ColumnProps) => {

    const context = React.useContext(DataPageContext);

    if (context.query.prototype instanceof QueryFor) {
        return (
            <DataTableForQuery {...context} selection={context.selectedItem} onSelectionChange={context.onSelectionChanged}>
                {children}
            </DataTableForQuery>);

    } else {
        return (
            <DataTableForObservableQuery {...context} selection={context.selectedItem} onSelectionChange={context.onSelectionChanged}>
                {children}
            </DataTableForObservableQuery>);
    }
};

export interface IDetailsComponentProps<TDataType> {
    item: TDataType;

}

interface IDataPageContext extends DataPageProps<any, any, any> {
    selectedItem: any;
    onSelectionChanged: (e: DataTableSelectionSingleChangeEvent<any>) => void;
}

const DataPageContext = React.createContext<IDataPageContext>(null as any);

/**
 * Props for the DataPage component
 */
export interface DataPageProps<TQuery extends IQueryFor<TDataType> | IObservableQueryFor<TDataType>, TDataType, TArguments> {
    /**
     * The title of the page
     */
    title: string;

    /**
     * Children to render, for this it means menu items and columns. Use <DataPage.MenuItems> and <DataPage.Columns> for this.
     */
    children: ReactNode;

    /**
     * Component to render when the selection changes
     */
    detailsComponent?: React.FC<IDetailsComponentProps<any>>;

    /**
     * The type of query to use
     */
    query: Constructor<TQuery>;

    /**
     * Optional arguments to pass to the query
     */
    queryArguments?: TArguments;

    /**
     * The message to show when there is no data
     */
    emptyMessage: string;

    /**
     * The key to use for the data
     */
    dataKey?: string | undefined;

    /**
     * The current selection.
     */
    selection?: any[number] | undefined | null;

    /**
     * Callback for when the selection changes
     */
    onSelectionChange?(event: DataTableSelectionSingleChangeEvent<any>): void;

    /**
     * Fields to use for global filtering
     */
    globalFilterFields?: string[] | undefined;

    /**
     * Default filters to use
     */
    defaultFilters?: DataTableFilterMeta;
}

/**
 * Represents a data driven page with a menu and custom defined columns for the data table.
 * @param props Props for the DataPage component
 * @returns Function to render the DataPage component
 */
const DataPage = <TQuery extends IQueryFor<TDataType> | IObservableQueryFor<TDataType, TArguments>, TDataType, TArguments extends object>(props: DataPageProps<TQuery, TDataType, TArguments>) => {
    const [selectedItem, setSelectedItem] = React.useState(undefined);

    const selectionChanged = (e: DataTableSelectionSingleChangeEvent<any>) => {
        setSelectedItem(e.value);
        if (props.onSelectionChange) {
            props.onSelectionChange(e);
        }
    };

    const context = { ...props, selectedItem, onSelectionChanged: selectionChanged };

    return (
        <DataPageContext.Provider value={context}>
            <Page title={props.title}>
                <Allotment className="h-full" proportionalLayout={false}>
                    <Allotment.Pane className="flex-grow">
                        {props.children}
                    </Allotment.Pane>
                    {props.detailsComponent && selectedItem &&
                        <Allotment.Pane preferredSize="450px">
                            <props.detailsComponent item={selectedItem} />
                        </Allotment.Pane>
                    }
                </Allotment>
            </Page>
        </DataPageContext.Provider>
    );
};

DataPage.MenuItems = MenuItems;
DataPage.Columns = Columns;

export { DataPage };
