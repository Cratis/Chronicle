param(
    [string]$NuspecPath,
    [string]$PackageId = "Cratis.Chronicle",
    [string]$PackageVersion = "1.0.0"
)

$xml = [xml](Get-Content $NuspecPath)
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('ns', $xml.DocumentElement.NamespaceURI)
$groups = $xml.SelectNodes('//ns:dependencies/ns:group', $ns)

foreach ($group in $groups) {
    $dep = $xml.CreateElement('dependency', $xml.DocumentElement.NamespaceURI)
    $dep.SetAttribute('id', $PackageId)
    $dep.SetAttribute('version', $PackageVersion)
    $dep.SetAttribute('exclude', 'Build,Analyzers')
    $group.AppendChild($dep) | Out-Null
}

$xml.Save($NuspecPath)
Write-Host "Successfully injected $PackageId dependency with version $PackageVersion"
