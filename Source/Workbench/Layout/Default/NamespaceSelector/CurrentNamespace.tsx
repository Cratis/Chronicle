// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import css from "./NamespaceSelector.module.css";
import { MdKeyboardArrowDown } from "react-icons/md";
import { HTMLAttributes, useEffect, useRef, useState } from "react";
import { Tooltip } from "@cratis/components/Common";

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
        if( name === undefined || name === null || name === '' ) return '';

        const names = name.split(/-|\s|(?=[A-Z])/);
        let initials = names[0].substring(0, 1).toUpperCase();
        if (names.length > 1) {
            initials += names[names.length - 1].substring(0, 1).toUpperCase();
        }
        return initials;
    };
    if (compact) {
        return (
            <div className={css.smallCurrentNamespace} {...rest} >
                <Tooltip content={namespace}>
                    <div className={css.smallNamespaceWrapper}>
                        <span className={css.namespaceName}>{getInitials(namespace)}</span>
                    </div>
                </Tooltip>
            </div>
        );
    }
    return (
        <Tooltip content={namespace} disabled={!isEllipsisActive}>
            <div className={`${css.currentNamespace}`} {...rest}>
                <span className={css.namespaceName} ref={namespaceNameRef}>{namespace}</span>
                <span><MdKeyboardArrowDown size={25}/></span>
            </div>
        </Tooltip>
    );
};
