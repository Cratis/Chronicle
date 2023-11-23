import { ITenant } from "./TenantSelector";
import css from "./TenantSelector.module.css";

interface ITenantListItemProps {
    tenant: ITenant;
    onClick: () => void;
}

export const TenantListItem = ({ tenant, onClick }: ITenantListItemProps) => {
    return <li onClick={onClick} className={`p-2 ${css.tenantListItem}`}>
        {tenant.name}
    </li>;
}