// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import css from "./TenantSelector.module.css";
import { MdKeyboardArrowDown } from "react-icons/md";
import { ITenant } from "./TenantSelector";
import { HTMLAttributes, useEffect, useRef, useState } from "react";
import { Tooltip } from "primereact/tooltip";

export interface ICurrentTenantProps extends HTMLAttributes<HTMLDivElement> {
    tenant: ITenant;
    compact?: boolean;
}

export const CurrentTenant = ({ tenant, compact, ...rest }: ICurrentTenantProps) => {
    const tenantNameRef = useRef<HTMLSpanElement>(null);
    const [isEllipsisActive, setIsEllipsisActive] = useState(false);

    useEffect(() => {
        if (tenantNameRef.current) {
            setIsEllipsisActive(tenantNameRef.current.offsetWidth < tenantNameRef.current.scrollWidth);
        }
    }, [tenant.name]);

    const getInitials = (name: string) => {
        const names = name.split(' ');
        let initials = names[0].substring(0, 1).toUpperCase();
        if (names.length > 1) {
            initials += names[names.length - 1].substring(0, 1).toUpperCase();
        }
        return initials;
    }
    if (compact) {
        return <>
            <Tooltip target={`.${css.smallTenantWrapper}`}/>
            <div className={css.smallCurrentTenant} {...rest} >
                <div className={css.smallTenantWrapper} data-pr-tooltip={tenant.name}>
                    <span className={css.tenantName}>{getInitials(tenant.name)}</span>
                </div>
            </div>
        </>
    }
    return <>
        {isEllipsisActive && <Tooltip target={`.${css.currentTenant}`}/>}
        <div className={`${css.currentTenant}`} {...rest} data-pr-tooltip={tenant.name}>
            <span className={css.tenantName} ref={tenantNameRef}>{tenant.name}</span>
            <span><MdKeyboardArrowDown size={25}/></span>
        </div>
    </>;
}
