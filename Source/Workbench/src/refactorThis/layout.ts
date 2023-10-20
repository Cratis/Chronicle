import React, { Dispatch, SetStateAction, HTMLAttributeAnchorTarget, } from 'react';

export type LayoutConfig = {
    inputStyle: string;
    colorScheme: string;
    theme: string;
    scale: number;
};

export interface LayoutContextProps {
    layoutConfig: LayoutConfig;
    setLayoutConfig: Dispatch<SetStateAction<LayoutConfig>>;
}

export interface AppConfigProps {
    simple?: boolean;
}

type CommandProps = {
    originalEvent: React.MouseEvent<HTMLAnchorElement, MouseEvent>;
    item: AppMenuItem;
};
export interface MenuModel {
    label: string;
    icon?: string;
    items?: MenuModel[];
    to?: string;
    url?: string;
    target?: HTMLAttributeAnchorTarget;
    separator?: boolean;
}
export interface AppMenuItem extends MenuModel {
    items?: AppMenuItem[];
    badge?: 'UPDATED' | 'NEW';
    badgeClass?: string;
    class?: string;
    preventExact?: boolean;
    visible?: boolean;
    disabled?: boolean;
    replaceUrl?: boolean;
    command?: ({ originalEvent, item }: CommandProps) => void;
}
export interface AppMenuItemProps {
    item?: AppMenuItem;
    parentKey?: string;
    index?: number;
    root?: boolean;
    className?: string;
}
