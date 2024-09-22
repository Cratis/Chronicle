// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useMemo } from 'react';
import { Page } from '../Common/Page';
import React from 'react';
import { MenuItem as PrimeMenuItem } from 'primereact/menuitem';
import { Menubar } from 'primereact/menubar';
import { IObservableQueryFor, IQueryFor, QueryFor } from '@cratis/applications/queries';
import { DataTableForObservableQuery, DataTableForObservableQueryProps } from '../DataTables/DataTableForObservableQuery';
import { DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { DataTableForQuery } from '../DataTables/DataTableForQuery';


export interface MenuItemProps extends PrimeMenuItem {
    disableOnUnselected?: boolean;
}

export const MenuItem = (props: MenuItemProps) => {
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

interface IDataPageContext extends DataPageProps<any, any, any> {
    selectedItem: any;
    onSelectionChanged: (e: DataTableSelectionSingleChangeEvent<any>) => void;
}

const DataPageContext = React.createContext<IDataPageContext>(null as any);

/**
 * Props for the DataPage component
 */
export interface DataPageProps<TQuery extends IQueryFor<TDataType> | IObservableQueryFor<TDataType>, TDataType, TArguments> extends DataTableForObservableQueryProps<TQuery, TDataType, TArguments> {
    /**
     * The title of the page
     */
    title: string;

    /**
     * Children to render, for this it means menu items and columns. Use <DataPage.MenuItems> and <DataPage.Columns> for this.
     */
    children: ReactNode;
}

/**
 * Represents a data driven page with a menu and custom defined columns for the data table.
 * @param props Props for the DataPage component
 * @returns Function to render the DataPage component
 */
const DataPage = <TQuery extends IQueryFor<TDataType> | IObservableQueryFor<TDataType, TArguments>, TDataType, TArguments extends {}>(props: DataPageProps<TQuery, TDataType, TArguments>) => {
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
                {props.children}
            </Page>
        </DataPageContext.Provider>
    );
};

DataPage.MenuItems = MenuItems;
DataPage.Columns = Columns;

export { DataPage };
