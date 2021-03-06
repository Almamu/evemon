# EVEMon XML Data Download Script

$sourceServer = "eve.no-ip.de"
$eveClientVer = "dom103"
$destination = "C:\Users\Richard Slater\Documents\Projects\EVEMon\Tools\Input\" # Change This!
$tablesRequired = "chrFactions",
                  "dgmAttributeCategories",
                  "dgmAttributeCategories",
                  "dgmAttributeTypes",
                  "dgmTypeAttributes",
                  "dgmTypeEffects",
                  "eveGraphics",
                  "eveUnits",
                  "invGroups",
                  "invMarketGroups",
                  "invMetaTypes",
                  "invTypes",
                  "mapConstellations",
                  "mapRegions",
                  "mapSolarSystemJumps",
                  "mapSolarSystems",
                  "staStations",
                  "ramTypeRequirements",
                  "crtCategories",
                  "crtCertificates", 
                  "crtClasses",
                  "crtRecommendations",
                  "crtRelationships"

Remove-Item "$destination*.bz2"
Remove-Item "$destination*.xml"

$webClient = New-Object System.Net.WebClient
$urlFormat = "http://{0}/{1}/{1}-mysql5-xml-v1/{1}-{2}-mysql5-v1.xml.bz2"
$fileFormat = "{0}{1}.xml.bz2"

foreach ($fileName in $tablesRequired)
{
    $fileSource = [String]::Format($urlFormat, $sourceServer, $eveClientVer, $filename)
    $fileDestination = [String]::Format($fileFormat, $destination, $filename)
    Write-Host -NoNewline "Downloading $filename from $sourceServer"
    $webClient.DownloadFile($fileSource, $fileDestination)
    Write-Host -ForegroundColor DarkGreen " [Finished]"
}