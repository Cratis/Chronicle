import { IMenuItemGroup, MenuItem } from "../MenuItem/MenuItem";
import css from "./MenuItemGroup.module.css";
import { useLayoutContext } from "../../context/LayoutContext";

interface IMenuItemGroupProps {
    group: IMenuItemGroup;
    basePath?: string;
}

export const MenuItemGroup = ({ group, basePath }: IMenuItemGroupProps) => {
    const { layoutConfig } = useLayoutContext()
    return <>
        {group.label &&
            <div className={`${css.label} ${!layoutConfig.leftSidebarOpen && 'invisible'} mb-2`}>{group.label}</div>}
        {group.items.map((item, index) => {
                return <MenuItem key={index} item={item} basePath={basePath}/>
            }
        )}
    </>;
}
