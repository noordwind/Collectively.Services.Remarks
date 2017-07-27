#!/bin/bash
export ASPNETCORE_ENVIRONMENT=local
cd src/Collectively.Services.Remarks
dotnet run --urls "http://*:10003"