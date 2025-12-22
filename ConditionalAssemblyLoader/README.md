# ConditionalAssemblyLoader
ConditionalAssemblyLoader is a library for conditional loading of implementation assemblies for an interface assembly.
The goal is to allow users to isolate code that would otherwise change from version to version
and reference version-specific code via dependency injection.

## Requirements
- .NET Standard 2.0

## Usage
Using ConditionalAssemblyLoader requires at least two projects:
- The **interface project**, which provides the public API and is referenced by other projects. It produces the **interface assembly**.
- The other projects are **implementation projects**, which reference the interface and provide the implementation to support the public API.
  They produce the **implementation assemblies**, one of which is loaded by ConditionalAssemblyLoader depending on the conditions.

1. Reference `ConditionalAssemblyLoader` in the interface project.
    - Option 1: Directly reference `ConditionalAssemblyLoader.dll`.
    - Option 2: Install the `ConditionalAssemblyLoader` NuGet package.
2. Create a type that derives from `AssemblyLoader\<T>` in the interface project. This is the actual **assembly loader**.
    - `AssemblyLoader\<T>` accepts a generic type parameter. The type represents the **entry point** into an implementation assembly and must be a class or interface.
    - Implement `OnAssemblyLoaded()` to perform any follow-up actions required to use the implementation assembly.
3. Reference the interface project in the implementation projects.
4. Create a type that derives from the entry point type in the implementation project.
5. Define a list of `ConditionalAssemblyReference`s for the assembly loader.
    - This can be done within the assembly loader type itself or externally by the user of the assembly loader.
6. Use the assembly loader, either within the interface project or in a different project that references the interface project.

## Example
Interface project:
```csharp

```

Implementation project:
```csharp

```