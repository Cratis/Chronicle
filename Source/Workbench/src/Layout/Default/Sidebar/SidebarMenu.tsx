import { IMenuItem, MenuItem } from "./MenuItem/MenuItem";

interface ILeftMenuProps {
    items: IMenuItem[];
    basePath?: string;
}

export const SidebarMenu = ({ items, basePath }: ILeftMenuProps) => {
    return <div>
        {items.map((item, index) => {
            return <MenuItem item={item} key={index} basePath={basePath}/>
        })}
    </div>;
}
