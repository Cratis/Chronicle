// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import css from './ItemsList.module.css';

export type ItemClicked<TItem> = (item: TItem) => void;

export interface IItemsListProps<TItem> {
    items: TItem[];
    idProperty?: keyof TItem;
    nameProperty?: keyof TItem;
    onItemClicked?: ItemClicked<TItem>;
}

export const ItemsList = <TItem extends {}>(props: IItemsListProps<TItem>) => {

    const getKey = (item: TItem): string => {
        if (props.idProperty) {
            return (item[props.idProperty] as any).toString();
        }
        return (item as any).toString();
    }

    const getName = (item: TItem):string => {
        if (props.nameProperty) {
            return (item[props.nameProperty] as any).toString();
        }
        return (item as any).toString();
    }

    return (
        <ul className={css.list}>
            {props.items.map((item) => {
                return (
                    <li key={getKey(item)} onClick={() => props.onItemClicked?.(item)} className={`p-2 ${css.listItem}`}>
                        {getName(item)}
                    </li>)
            })}
        </ul>
    );
};

