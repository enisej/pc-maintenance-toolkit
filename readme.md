# PC Maintenance Toolkit

A **.NET 9.0 console app** for running PC maintenance tasks (SFC, Power Report, Disk Check) and **custom PowerShell commands** — all **saved in PostgreSQL** with full logging.

## Stack

C# .NET 9.0
Entity Framework Core
Npgsql (PostgreSQL)
Console App

## Features

- Run **SFC /scannow**, **battery report**, **disk usage**  
- Add, edit, delete **any PowerShell command**  
- Full **execution history** with output  
- **Interactive log viewer** (choose ID → see full output)  
- **SortOrder** controls menu order  
- **UTC-safe** + **null-byte-safe**  
- Built with **EF Core + PostgreSQL**

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (local or remote)
- Visual Studio

## Setup

1. **Clone the repo**
   ```bash
   git clone https://github.com/yourname/PcMaintenanceToolkit.git
   cd PcMaintenanceToolkit
   ```

2. **Update appsettings.json**
```bash
{
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "pc_maintenance_db",
    "Username": "postgres",
    "Password": "yourpassword"
  }
}
```

3. **Run migrations**
```bash
dotnet ef database update
```

4. **Run the app**
```bash
dotnet run
```

## Usage

1. Run Command → Choose ID
2. View Logs → Choose ID to see full output
3. Manage Commands → Add/Edit/Delete
0. Exit

### Run as Administrator for SFC scan. !!!

## Add Custom Command

Name: Update Apps
Type: PowerShell
Script: choco upgrade all -y
SortOrder: 40

## Build & Publish
```bash
dotnet publish -c Release -o publish
```
Create pcmt.bat in PATH:
```bash
@echo off
"C:\path\to\publish\PcMaintenanceToolkit.exe" %*
```
Now run from CMD: pcmt