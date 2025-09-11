#!/bin/bash

# Usage: ./BuildAndPublish.sh [Release|Debug] [NugetRepo] [NugetRepoApiKey]
configuration="${1:-Debug}"
nugetRepo="${2:-LocalNugetRepo}"
nugetRepoApiKey="${3:-P@ssw0rd}"

get_project_version() {
    project_path="$1"
    project_name="$2"
    project_file="$project_path/$project_name.csproj"
    version=$(grep -oPm1 "(?<=<Version>)[^<]+" "$project_file")
    if [ -z "$version" ]; then
        version="1.0.0"
    fi
    echo "$version"
}

build_and_publish() {
    project_name="$1"
    project_version="$2"
    project_path="$3"
    configuration="$4"
    nugetRepo="$5"
    nugetRepoApiKey="$6"

    echo "Building $project_name"
    project_file="$project_path/$project_name.csproj"
    dotnet build "$project_file" -c "$configuration"

    echo "Publishing $project_name"
    package_file="$project_path/bin/$configuration/$project_name.$project_version.nupkg"
    dotnet nuget push "$package_file" --source "$nugetRepo" --api-key "$nugetRepoApiKey"
}

projects=(
    "AGTec.Common.Base:../src/Common/Base"
    "AGTec.Common.Domain:../src/Common/Domain"
    "AGTec.Common.BackgroundTaskQueue:../src/Common/BackgroundTaskQueue"
    "AGTec.Common.Document:../src/Common/Document"
    "AGTec.Common.CQRS:../src/Common/CQRS"
    "AGTec.Common.CQRS.Messaging.AzureServiceBus:../src/Common/CQRS.Messaging.AzureServiceBus"
    "AGTec.Common.CQRS.Messaging.JsonSerializer:../src/Common/CQRS.Messaging.JsonSerializer"
    "AGTec.Common.CQRS.Messaging.ProtoBufSerializer:../src/Common/CQRS.Messaging.ProtoBufSerializer"
    "AGTec.Common.Monitor:../src/Common/Monitor"
    "AGTec.Common.Randomizer:../src/Common/Randomizer"
    "AGTec.Common.Repository:../src/Common/Repository"
    "AGTec.Common.Repository.Document:../src/Common/Repository.Document"
    "AGTec.Common.Repository.Search:../src/Common/Repository.Search"
    "AGTec.Common.Test:../src/Common/Test"
    "AGTec.Services.ServiceDefaults:../src/Services/ServiceDefaults"
)

for entry in "${projects[@]}"; do
    project_name="${entry%%:*}"
    project_path="$(pwd)/${entry#*:}"
    project_version=$(get_project_version "$project_path" "$project_name")
    build_and_publish "$project_name" "$project_version" "$project_path" "$configuration" "$nugetRepo" "$nugetRepoApiKey"
done
