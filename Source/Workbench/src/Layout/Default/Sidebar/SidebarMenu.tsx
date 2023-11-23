import { IMenuItemGroup } from "./MenuItem/MenuItem";
import { MenuItemGroup } from "./MenuItemGroup/MenuItemGroup";

interface ILeftMenuProps {
    items: IMenuItemGroup[];
    basePath?: string;
}


export const SidebarMenu = ({ items, basePath }: ILeftMenuProps) => {
    return items.map((group, index) => {
            return <MenuItemGroup key={index} group={group} basePath={basePath}/>
        }
    )
}
