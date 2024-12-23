param(
    [ValidateSet("Release", "Debug")]$configuration = "Release",
    [string]$nugetRepo = "LocalNugetRepo",
    [string]$nugetRepoApiKey = "P@ssw0rd"
)

function BuildAndPublish {
    param(
        [string]$projectName,
        [string]$projectVersion,
        [string]$projectPath,
        [string]$configuration,
        [string]$nugetRepo,
        [string]$nugetRepoApiKey
    )

    Write-Host "Building $projectName"
    $projectFile = [IO.Path]::Combine($projectPath, $projectName + ".csproj")
    dotnet build $projectFile -c $configuration

    Write-Host "Publishing $projectName"
    $packageFile = [IO.Path]::Combine($projectPath, "bin", $configuration, $projectName + "." + $projectVersion + ".nupkg")
    dotnet nuget push $packageFile --source $nugetRepo --api-key $nugetRepoApiKey
}

function GetProjectVersion {
    param(
        [string]$projectPath,
        [string]$projectName
    )

    $projectFile = [IO.Path]::Combine($projectPath, $projectName + ".csproj")
    $projectVersion = [xml](Get-Content $projectFile).Project.PropertyGroup.Version
    $result = If ($nul -eq $projectVersion) { "1.0.0" } Else { $projectVersion }
    return $result
}


$projects = @(
    @{
        Name = "AGTec.Common.Base"
        Path = "src\Common\Base"
    },
    @{
        Name = "AGTec.Common.Domain"
        Path = "src\Common\Domain"
    },
    @{
        Name = "AGTec.Common.BackgroundTaskQueue"
        Path = "src\Common\BackgroundTaskQueue"
    },
    @{
        Name = "AGTec.Common.Document"
        Path = "src\Common\Document"
    },
    @{
        Name = "AGTec.Common.CQRS"
        Path = "src\Common\CQRS"
    },
    @{
        Name = "AGTec.Common.CQRS.Messaging.ActiveMQ"
        Path = "src\Common\CQRS.Messaging.ActiveMQ"
    },
    @{
        Name = "AGTec.Common.CQRS.Messaging.AzureServiceBus"
        Path = "src\Common\CQRS.Messaging.AzureServiceBus"
    },
    @{
        Name = "AGTec.Common.CQRS.Messaging.JsonSerializer"
        Path = "src\Common\CQRS.Messaging.JsonSerializer"
    },
    @{
        Name = "AGTec.Common.CQRS.Messaging.ProtoBufSerializer"
        Path = "src\Common\CQRS.Messaging.ProtoBufSerializer"
    },
    @{
        Name = "AGTec.Common.HttpClient"
        Path = "src\Common\HttpClient"
    },
    @{
        Name = "AGTec.Common.Monitor"
        Path = "src\Common\Monitor"
    },
    @{
        Name = "AGTec.Common.Randomizer"
        Path = "src\Common\Randomizer"
    },
    @{
        Name = "AGTec.Common.Repository"
        Path = "src\Common\Repository"
    },
    @{
        Name = "AGTec.Common.Repository.Document"
        Path = "src\Common\Repository.Document"
    },
    @{
        Name = "AGTec.Common.Repository.Search"
        Path = "src\Common\Repository.Search"
    },
    @{
        Name = "AGTec.Common.SignalR"
        Path = "src\Common\SignalR"
    },
    @{
        Name = "AGTec.Common.Test"
        Path = "src\Common\Test"
    },
    @{
        Name = "AGTec.Worker"
        Path = "src\Worker"
    },
    @{
        Name = "AGTec.Microservice"
        Path = "src\Microservice"
    }
)

foreach ($project in $projects) {
    $projectName = $project.Name
    $projectPath = [IO.Path]::Combine($PWD, $project.Path)
    $projectVersion = GetProjectVersion -projectPath $projectPath -projectName $projectName
    BuildAndPublish -projectName $projectName -projectVersion $projectVersion -projectPath $projectPath -configuration $configuration -nugetRepo $nugetRepo -nugetRepoApiKey $nugetRepoApiKey
}
