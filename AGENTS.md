# RepyPharma Codex Instructions

## Project Overview
- RepyPharma is an ASP.NET Core Blazor Server app targeting `.NET 8`.
- UI uses Razor components under `Components/`, Microsoft Fluent UI components, and ApexCharts.
- Domain code is organized around pharmacy stock, minimum stock, orders, replenishment, PDF parsing, and reports.
- Services are registered in `Program.cs`; keep dependency registration there when adding new app services.

## Repository Layout
- `Components/`: Blazor app shell, layouts, pages, and component-specific CSS.
- `Components/Pages/Replacement/`: replenishment dashboard and grid workflow.
- `Models/`: domain models and report DTOs.
- `Services/Interfaces/`: service contracts.
- `Services/Implementations/`: application service implementations.
- `Services/Abstractions/`: shared support services and UI/application helpers.
- `ViewModels/`: view model definitions.
- `wwwroot/`: static assets.
- `storage/`: local runtime/storage data; avoid committing generated or user-uploaded data unless explicitly requested.

## Commands
Run commands from the Git root:

```bash
cd /mnt/A41E89C91E899548/Projects/RepyPharma/RepyPharma
dotnet restore
dotnet build
dotnet run
```

There is currently no dedicated test project in the repository. If tests are added, prefer `dotnet test` from this root.

## Coding Guidelines
- Preserve nullable reference type safety and implicit using conventions from `RepyPharma.csproj`.
- Prefer dependency injection through interfaces in `Services/Interfaces/` for business workflows.
- Keep UI state and rendering behavior in Razor components; keep stock/order/replenishment rules in services.
- Use component-scoped `.razor.css` files for page or component styling when possible.
- Match existing Fluent UI patterns before introducing custom controls.
- Keep changes narrowly scoped and avoid unrelated formatting churn.

## Git and Local Changes
- The working tree may contain user changes. Do not revert or overwrite them unless explicitly asked.
- Before editing, check `git status --short`.
- Do not commit build outputs from `bin/` or `obj/`.

## Notes for Codex
- Treat the root Git project as `RepyPharma/RepyPharma`, not the outer solution folder.
- If a change touches UI behavior, run `dotnet build` at minimum.
- If a change affects PDF parsing, storage, stock calculations, or replenishment priority, add focused tests when a test project exists or document the manual verification performed.
