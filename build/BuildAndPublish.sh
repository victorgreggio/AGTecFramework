#!/bin/bash

# Usage: ./BuildAndPublish.sh [Release|Debug] [NugetRepo] [NugetRepoApiKey]
configuration="${1:-Debug}"
nugetRepo="${2:-LocalNugetRepo}"
nugetRepoApiKey="${3:-P@ssw0rd}"

ensure_nuget_source() {
    source_name="$1"
    
    echo "Checking if NuGet source '$source_name' exists..."
    sources=$(dotnet nuget list source)
    
    if ! echo "$sources" | grep -q "$source_name"; then
        echo "NuGet source '$source_name' not found. Creating..."
        
        nuget_repo_path="$HOME/$source_name"
        
        if [ ! -d "$nuget_repo_path" ]; then
            echo "Creating folder: $nuget_repo_path"
            mkdir -p "$nuget_repo_path"
        fi
        
        echo "Adding NuGet source '$source_name' at: $nuget_repo_path"
        dotnet nuget add source "$nuget_repo_path" --name "$source_name"
        echo "NuGet source '$source_name' added successfully."
    else
        echo "NuGet source '$source_name' already exists."
    fi
}

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

ensure_nuget_source "$nugetRepo"

projects=(
    "AGTec.Common.Base:../src/Common/Base"
    "AGTec.Common.Domain:../src/Common/Domain"
    "AGTec.Common.BackgroundTaskQueue:../src/Common/BackgroundTaskQueue"
    "AGTec.Common.Document:../src/Common/Document"
    "AGTec.Common.CQRS:../src/Common/CQRS"
    "AGTec.Common.CQRS.Messaging.AzureServiceBus:../src/Common/CQRS.Messaging.AzureServiceBus"
    "AGTec.Common.CQRS.Messaging.JsonSerializer:../src/Common/CQRS.Messaging.JsonSerializer"
    "AGTec.Common.CQRS.Messaging.ProtoBufSerializer:../src/Common/CQRS.Messaging.ProtoBufSerializer"
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
