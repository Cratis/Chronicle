import { ITenant } from "./TenantSelector";
import { InputText } from "primereact/inputtext";
import { useState } from "react";
import { TenantListItem } from "./TenantListItem";
import css from "./TenantSelector.module.css";

interface ISelectTenantProps {
    onSelected: (tenant: ITenant) => void;
}

export const SelectTenant = ({ onSelected }: ISelectTenantProps) => {
    const [search, setSearch] = useState<string>('');
    const allTenants: ITenant[] = [
        {
            id: '1',
            name: 'Drammen Kommunale Pensjonskasse'
        },
        {
            id: '2',
            name: 'Arendal Kommunale Pensjonskasse'
        },
        {
            id: '3',
            name: 'Viken Fylkeskommunale Pensjonskasse'
        },
        {
            id: '4',
            name: 'Nordland Fylkeskommunale Pensjonskasse'
        },
        {
            id: '5',
            name: 'Bergen Kommunale Pensjonskasse'
        },
        {
            id: '6',
            name: 'Oslo Kommunale Pensjonskasse'

        },
        {
            id: '7',
            name: 'Trondheim Kommunale Pensjonskasse'
        }

        ];

    return <div>
        <div className={'mb-2'}>
            <InputText value={search}
                       placeholder={'Search for tenant'}
                       onChange={(e) => {
                           setSearch(e.target.value)
                       }}/>
        </div>
        <ul className={css.tenantList}>
            {allTenants.filter((t) => t.name.toLowerCase().includes(search.toLowerCase())).map((tenant) => {
                return <TenantListItem tenant={tenant} onClick={() => onSelected(tenant)} key={tenant.id}/>;
            })}
        </ul>
    </div>;
}
