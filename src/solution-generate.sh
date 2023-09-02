#!/bin/bash

export SLN=Nowy.Database
export SLN_TEMP=${SLN}.TEMP

dotnet new sln --name ${SLN_TEMP}
dotnet sln ${SLN_TEMP}.sln add ../nukebuild/_build.csproj
dotnet sln ${SLN_TEMP}.sln add */*.csproj
mv ${SLN_TEMP}.sln ${SLN}.sln



export SLN=Nowy.Database.FULL
export SLN_TEMP=${SLN}.TEMP

dotnet new sln --name ${SLN_TEMP}
dotnet sln ${SLN_TEMP}.sln add ../nukebuild/_build.csproj
dotnet sln ${SLN_TEMP}.sln add */*.csproj
mv ${SLN_TEMP}.sln ${SLN}.sln

