#!/bin/sh

if [ -z "$1" ]
then
  echo No version specified! Please specify a valid version like 1.2.3!
  exit 1
else
  echo version $1
fi

echo Cleaning up
rm -r ./dist

echo Restore solution
dotnet restore microbus.sln

echo Packaging solution
dotnet pack src/Core/ -c Release /p:PackageVersion=$1 /p:Version=$1 -o ./dist/nupkgs/

if [ -z "$2" ]
then
  echo Done
  exit 0
fi

for package in $(find ./dist/nupkgs/ -name *.nupkg); do
  echo Pushing $package
  dotnet nuget push $package -k $2 -s https://api.nuget.org/v3/index.json
done

echo Done
