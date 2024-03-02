// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import css from './ItemsList.module.css';

export type ItemClicked<TItem> = (item: TItem) => void;

export interface IItemsListProps<TItem> {
    items: TItem[];
    idProperty?: keyof TItem;
    titleProperty?: keyof TItem;
    onItemClicked?: ItemClicked<TItem>;
}

export const ItemsList = <TItem extends {}>(props: IItemsListProps<TItem>) => {

    const getKey = (item: TItem): string => {
        if (props.idProperty) {
            return (item[props.idProperty] as any).toString();
        }
        return (item as any).toString();
    }

    const getTitle = (item: TItem):string => {
        if (props.titleProperty) {
            return (item[props.titleProperty] as any).toString();
        }
        return (item as any).toString();
    }

    return (
        <ul className={css.list}>
            {props.items.map((item) => {
                return (
                    <li key={getKey(item)} onClick={() => props.onItemClicked?.(item)} className={`p-2 ${css.listItem}`}>
                        {getTitle(item)}
                    </li>)
            })}
        </ul>
    );
};

