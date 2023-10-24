import { useDarkMode } from "usehooks-ts";
import { Button } from "primereact/button";

export const ThemeSwitch = () => {
    const { isDarkMode, toggle } = useDarkMode()
    return (
        <Button onClick={toggle} size="small" text>
            {isDarkMode ? 'Light' : 'Dark'}
        </Button>
    )
}