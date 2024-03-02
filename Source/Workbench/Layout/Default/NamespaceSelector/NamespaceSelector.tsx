// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from "react";
import css from './NamespaceSelector.module.css';
import { OverlayPanel } from "primereact/overlaypanel";
import { useLayoutContext } from "../context/LayoutContext";
import { CurrentNamespace } from "./CurrentNamespace";
import { SelectNamespace } from "./SelectNamespace";

export interface INamespace {
    id: string;
    name: string;
}

export interface INamespaceSelectorProps extends React.HTMLAttributes<HTMLDivElement>{
    onNamespaceSelected: (namespace: INamespace) => void;
}

export const NamespaceSelector = ({ onNamespaceSelected: onNamespaceSelected, ...rest }: INamespaceSelectorProps) => {
    const { layoutConfig } = useLayoutContext();

    const op = useRef<OverlayPanel>(null);
    const [namespace, setNamespace] = useState<INamespace>({
        id: '1',
        name: 'Drammen Kommunale Pensjonskasse'
    });

    const selectNamespace = (namespace: INamespace) => {
        setNamespace(namespace);
        op?.current?.hide();
    }

    useEffect(() => {
        onNamespaceSelected(namespace);
    }, [namespace]);
    return (
        <div {...rest}>
            <CurrentNamespace compact={!layoutConfig.leftSidebarOpen}
                           namespace={namespace} onClick={(e) => {
                op?.current?.toggle(e, null)
            }}/>

            <OverlayPanel ref={op}
                          className={`${css.overlayPanel} ${layoutConfig.leftSidebarOpen ? css.openOverlayPanel : css.closedOverlayPanel}`}>
                <SelectNamespace onSelected={selectNamespace}/>
            </OverlayPanel>
        </div>);
}
