// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import css from "./NamespaceSelector.module.css";
import { MdKeyboardArrowDown } from "react-icons/md";
import { HTMLAttributes, useEffect, useRef, useState } from "react";
import { Tooltip } from "primereact/tooltip";

export interface ICurrentNamespaceProps extends HTMLAttributes<HTMLDivElement> {
    namespace: string;
    compact?: boolean;
}

export const CurrentNamespace = ({ namespace: namespace, compact, ...rest }: ICurrentNamespaceProps) => {
    const namespaceNameRef = useRef<HTMLSpanElement>(null);
    const [isEllipsisActive, setIsEllipsisActive] = useState(false);

    useEffect(() => {
        if (namespaceNameRef.current) {
            setIsEllipsisActive(namespaceNameRef.current.offsetWidth < namespaceNameRef.current.scrollWidth);
        }
    }, [namespace]);

    const getInitials = (name: string) => {
        const names = name.split(/-|\s|(?=[A-Z])/);
        let initials = names[0].substring(0, 1).toUpperCase();
        if (names.length > 1) {
            initials += names[names.length - 1].substring(0, 1).toUpperCase();
        }
        return initials;
    }
    if (compact) {
        return <>
            <Tooltip target={`.${css.smallNamespaceWrapper}`}/>
            <div className={css.smallCurrentNamespace} {...rest} >
                <div className={css.smallNamespaceWrapper} data-pr-tooltip={namespace}>
                    <span className={css.namespaceName}>{getInitials(namespace)}</span>
                </div>
            </div>
        </>
    }
    return <>
        {isEllipsisActive && <Tooltip target={`.${css.currentNamespace}`}/>}
        <div className={`${css.currentNamespace}`} {...rest} data-pr-tooltip={namespace}>
            <span className={css.namespaceName} ref={namespaceNameRef}>{namespace}</span>
            <span><MdKeyboardArrowDown size={25}/></span>
        </div>
    </>;
}
