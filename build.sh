#!/bin/sh

if [ -z "$1" ] 
then
  echo No version specified! Please specify a valid version like 1.2.3!
  exit 1
else
  echo version $1
fi

echo Restore solution
dotnet restore microbus.sln

echo Packaging solution
dotnet pack src/Core/ -c Release /p:PackageVersion=$1 -o ./../../dist/nupkgs

echo Done
