#!/bin/bash
echo "The installed .Net version number is: " $(dotnet --version)
echo "1. installing NodeJs..."
sudo apt install nodejs
echo "2. installing npm..."
sudo apt install npm
echo "3. installing ef tools..."
dotnet tool install --global dotnet-ef
echo "4. Building XtraUpload (Release version)..."
dotnet publish --configuration Release
echo "5. Building initial database migration"
dotnet ef migrations add initCommit -p ./Database/XtraUpload.Database.Migrations -s XtraUpload.WebApp
echo "6. Generating sql script"
dotnet ef migrations script -o ./Database/XtraUpload.Database.Migrations/script.sql -p ./Database/XtraUpload.Database.Migrations -s XtraUpload.WebApp
echo "The sql script [script.sql] has been successfully generated, path: ./Database/XtraUpload.Database.Migrations/"
echo "7. updating db..."
dotnet ef database update initCommit -p ./Database/XtraUpload.Database.Migrations -s XtraUpload.WebApp
buildDir="/var/wwwroot"
echo "8. Moving build directory to " + $buildDir
mkdir -p $buildDir; mv ./XtraUpload.WebApp/bin/Release/netcoreapp3.1/publish/* $_
rm ./XtraUpload.WebApp/bin/*
echo "9. starting web server..."
cd $buildDir
dotnet XtraUpload.WebApp.dll
