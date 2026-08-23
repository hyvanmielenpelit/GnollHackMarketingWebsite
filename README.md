# GnollHack.com Marketing Website

![GnollHack Logo](https://images.gnollhack.com/marketing/gnollhack-icon-v2-256.png)

## Overview

The official marketing website for [GnollHack](https://gnollhack.com), the modern roguelike game.

## Technology Stack

- **Framework**: ASP.NET Core 10.0 (Razor Pages)
- **Frontend**: Bootstrap 5.3.3, SCSS, JavaScript (ES6+), jQuery 3.7.1
- **CDN / Caching**: Azure Front Door (AFD)
- **Deployment Target**: `win-x64` (Framework-dependent)

## Building and Running

To build and run locally:

```powershell
dotnet build GnollHackMarketingWebsite.slnx
dotnet run --project GnollHackMarketingWebsite
```

## Publishing

To publish for production deployment:

```powershell
dotnet publish GnollHackMarketingWebsite\GnollHackMarketingWebsite.csproj /p:PublishProfile=FolderProfile
```

Output is generated at `GnollHackMarketingWebsite\bin\Release\net10.0\publish\win-x64\`.