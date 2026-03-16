# Copilot instructions for FirstGame1

This is a Unity project.

## Project structure
- Core game code is under `Assets/`.
- Unity package configuration is under `Packages/`.
- Unity project settings are under `ProjectSettings/`.

## Development guidelines
- Make focused, minimal changes that directly address the requested issue.
- Do not commit generated Unity artifacts (for example `Library/`, `Temp/`, `Obj/`, build outputs, or generated `*.csproj` files).
- Preserve existing gameplay behavior unless the requested task explicitly requires a behavior change.
- Follow existing C# and Unity patterns already used in the surrounding files.

## Validation guidance
- Prefer running tests that already exist for the area being changed.
- For script-level checks outside Unity Editor, `dotnet` commands may fail if Unity-generated project files are not present.
- When possible, validate Unity-related changes through existing Unity test workflows or editor play/testing flows.
