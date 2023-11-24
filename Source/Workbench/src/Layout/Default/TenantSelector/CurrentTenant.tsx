import css from "./TenantSelector.module.css";
import { MdKeyboardArrowDown } from "react-icons/md";
import { ITenant } from "./TenantSelector";
import { HTMLAttributes } from "react";
import { Tooltip } from "primereact/tooltip";

export interface ICurrentTenantProps extends HTMLAttributes<HTMLDivElement> {
    tenant: ITenant;
    compact?: boolean;
}

export const CurrentTenant = ({ tenant, compact, ...rest }: ICurrentTenantProps) => {
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
            <Tooltip target={`.${css.smallCurrentTenant}`}/>
            <div className={css.smallCurrentTenant} {...rest} data-pr-tooltip={tenant.name}>
                <span className={css.tenantName}>{getInitials(tenant.name)}</span>
            </div>
        </>
    }
    return <div className={`${css.currentTenant}`} {...rest}>
        <span className={css.tenantName}>{tenant.name}</span>
        <span><MdKeyboardArrowDown size={25}/></span>
    </div>;
}
