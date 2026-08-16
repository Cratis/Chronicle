// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { type CSSProperties, type ReactNode, useMemo } from 'react';
import { Tabs as PrimeTabs } from 'primereact/tabs';

/**
 * Props for {@link TabPanel}.
 */
export interface TabPanelProps {
    /** The tab's label. */
    header: ReactNode;
    /** The panel content. */
    children?: ReactNode;
    /** Applied to this panel's content element. */
    contentStyle?: CSSProperties;
}

/**
 * Declares one tab and its panel.
 *
 * A pure marker: {@link Tabs} reads its props to build the tab list and the
 * matching panel, so it renders nothing when mounted on its own.
 */
export const TabPanel: React.FC<TabPanelProps> = () => null;
TabPanel.displayName = 'TabPanel';

/**
 * Props for {@link Tabs}.
 */
export interface TabsProps {
    /** `<TabPanel>` markers describing the tabs. */
    children?: ReactNode;
    /** Applied to the tabs root. */
    className?: string;
    /** Applied to the tabs root. */
    style?: CSSProperties;
    /** Applied to the element wrapping the panels. */
    panelContainerClassName?: string;
    /** Applied to the element wrapping the panels. */
    panelContainerStyle?: CSSProperties;
}

/**
 * A declarative tab strip over PrimeReact 11's compositional `Tabs` primitives.
 *
 * PrimeReact 10's `<TabView><TabPanel header=…>` pair became `Tabs.Root` +
 * `Tabs.List` + `Tabs.Tab` + `Tabs.Panels` + `Tabs.Panel`, where each tab and
 * panel are matched by an explicit `value`. This keeps the original authoring
 * model and derives those values from the child order.
 */
export const Tabs = ({ children, className, style, panelContainerClassName, panelContainerStyle }: TabsProps) => {
    const panels = useMemo(
        () => React.Children.toArray(children).filter(React.isValidElement) as React.ReactElement<TabPanelProps>[],
        [children]);

    return (
        <PrimeTabs.Root defaultValue={0} className={className} style={style}>
            <PrimeTabs.List>
                {panels.map((panel, index) => (
                    <PrimeTabs.Tab key={index} value={index}>
                        {panel.props.header}
                    </PrimeTabs.Tab>
                ))}
            </PrimeTabs.List>
            <PrimeTabs.Panels className={panelContainerClassName} style={panelContainerStyle}>
                {panels.map((panel, index) => (
                    <PrimeTabs.Panel key={index} value={index} style={panel.props.contentStyle}>
                        {panel.props.children}
                    </PrimeTabs.Panel>
                ))}
            </PrimeTabs.Panels>
        </PrimeTabs.Root>
    );
};
