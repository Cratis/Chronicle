// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMenuItemGroup } from "./MenuItem/MenuItem";
import { MenuItemGroup } from "./MenuItemGroup/MenuItemGroup";

interface ILeftMenuProps {
    items: IMenuItemGroup[];
    basePath?: string;
}

export const SidebarMenu = ({ items, basePath }: ILeftMenuProps) => {
    return items.map((group, index) => {
            return <MenuItemGroup key={index} group={group} basePath={basePath}/>;
        }
    );
};
