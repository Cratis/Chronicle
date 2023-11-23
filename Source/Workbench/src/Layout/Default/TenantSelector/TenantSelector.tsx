import { useEffect, useRef, useState } from "react";
import css from './TenantSelector.module.css';
import { OverlayPanel } from "primereact/overlaypanel";
import { useLayoutContext } from "../context/LayoutContext";
import { CurrentTenant } from "./CurrentTenant";
import { SelectTenant } from "./SelectTenant";

export interface ITenant {
    id: string;
    name: string;
}

export interface ITenantSelectorProps extends React.HTMLAttributes<HTMLDivElement>{
    onTenantSelected: (tenant: ITenant) => void;
}

export const TenantSelector = ({ onTenantSelected, ...rest }: ITenantSelectorProps) => {
    const { layoutConfig } = useLayoutContext();

    const op = useRef<OverlayPanel>(null);
    const [tenant, setTenant] = useState<ITenant>({
        id: '1',
        name: 'opensjon'
    });

    const selectTenant = (tenant: ITenant) => {
        setTenant(tenant);
        op?.current?.hide();
    }

    useEffect(() => {
        onTenantSelected(tenant);
    }, [tenant]);
    return (
        <div {...rest}>
            <CurrentTenant compact={!layoutConfig.leftSidebarOpen}
                           tenant={tenant} onClick={(e) => {
                op?.current?.toggle(e, null)
            }}/>

            <OverlayPanel ref={op}
                          className={`${css.overlayPanel} ${layoutConfig.leftSidebarOpen ? css.openOverlayPanel : css.closedOverlayPanel}`}>
                <SelectTenant onSelected={selectTenant}/>
            </OverlayPanel>
        </div>);
}