// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { generatePath, NavLink, useParams } from "react-router-dom";
import css from "./MenuItem.module.css";
import { Ripple } from "primereact/ripple";
import { IconType } from "react-icons/lib";
import { useLayoutContext } from "../../context/LayoutContext";
import { useContext, useEffect, useState } from "react";
import { MenuContext } from "../../context/MenuContext";

export interface IMenuItem {
    label: string;
    url: string;
    icon?: IconType;
}

export interface IMenuItemGroup {
    label?: string;
    items: IMenuItem[];
}

export interface IMenuItemProps {
    item: IMenuItem;
    basePath?: string;
}

export const MenuItem = ({ item, basePath, ...rest }: IMenuItemProps) => {
    const layoutContext = useLayoutContext();
    const params = useParams();
    const ctx = useContext(MenuContext);
    const itemPath = cleanupPath(basePath) + item.url;
    const resolvedPath = generatePath(itemPath ?? '', Object.assign({}, ctx.paramsFallback, params));

    const [labelClass, setLabelClass] = useState(css.label);
    useEffect(() => {
        setLabelClass(css.label + ' ' + (!layoutContext.layoutConfig.leftSidebarOpen ? css.hidden : ''));
    }, [layoutContext.layoutConfig.leftSidebarOpen]);

    return (
        <NavLink to={resolvedPath}
                 {...rest}
                 className={({ isActive, isPending }) =>
                     css.menuItem + ' ' + (isPending ? css.pending : isActive ? css.active : "") + " p-ripple "
                 }>
            <Ripple/>
            <div className={css.icon}>
                {item.icon && <item.icon size='1.5rem'/>}
            </div>
            <div className={labelClass}>
                {item.label}
            </div>

        </NavLink>

    );
};

function cleanupPath(path: string | undefined) {
    if (path && path.length > 1 && !path.endsWith('/')) {
        return path + '/';
    }
    return path;
}
