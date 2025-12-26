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
  They produce the **implementation assemblies**, one of which is loaded by ConditionalAssemblyLoader depending on the conditions at runtime.

First, reference `ConditionalAssemblyLoader` in the interface project.
- Option 1: Directly reference `ConditionalAssemblyLoader.dll`.
- Option 2: Install the `ConditionalAssemblyLoader` NuGet package.

In the interface project, create a type that derives from `AssemblyLoader<T>`. This will be used as the **assembly loader**.
- `AssemblyLoader<T>` accepts a generic type parameter. The type represents the **entry point** into an implementation assembly and must be a class or interface.
- Implement `OnAssemblyLoaded()` to perform any follow-up actions required to use the implementation assembly.
```csharp
public sealed class MyAssemblyLoader : AssemblyLoader<MyEntryPoint>
{
    // ... other members
    
    public override void OnAssemblyLoaded(MyEntryPoint entryPoint)
    {
        // ... code here
    }
}

public abstract class MyEntryPoint
{
    // ... members go here
}
```
Next, reference the interface project in the implementation projects and create a type that derives from the entry point type.
```csharp
public sealed class MyImplementationEntryPoint : MyEntryPoint
{
    // ... members + overrides go here
}
```
Back in the interface project, define a list of `ConditionalAssemblyReference`s for the assembly loader.
- This can be done within the assembly loader type itself or externally by the user of the assembly loader.
- A `ConditionalAssemblyReference` defines the conditions needed to use the referenced assembly it is associated with.
- Each reference is evaluated in the order they are defined in `References`. Newer implementations should be listed ahead of older ones.
```csharp
// in the assembly loader type
public sealed class MyAssemblyLoader : AssemblyLoader<MyEntryPoint>
{
    // ...
    
    public MyAssemblyLoader()
    {
        References.Add(new ConditionalAssemblyReference("MyImplementationAssembly", "Path/To/MyImplementationAssembly.dll"));
    }
}

// or externally
var loader = new MyAssemblyLoader();
loader.References.Add(new ConditionalAssemblyReference("MyImplementationAssembly", "Path/To/MyImplementationAssembly.dll"));
```
Finally, use the assembly loader, either within the interface project or in a different project that references the interface project.
```csharp
var loader = new MyAssemblyLoader();
if (loader.TryLoad(out var loadedAssembly, out var error))
{
    var entryPoint = loadedAssembly.Instance;
    // ... other code
}
// ... other code
```

## Mechanics
When the user attempts to load an implementation assembly through the created assembly loader (via `AssemblyLoader.TryLoad()`),
it will iterate through the list of `ConditionalAssemblyReference`s and attempt to load the referenced assembly.
During the process, it will attempt to resolve the name of the assembly, if supplied, by checking the directory
where the loader's assembly is located.
This is the default behavior of `AssemblyLoader.ResolveAssemblyAtLoad()`, which can be overridden by the user.
If it cannot do so, it will attempt to load by using the supplied path.

Once an implementation assembly is successfully loaded, the loader will proceed with creating an instance of
the entry point type. It will attempt to resolve assemblies by checking the currently loaded assemblies.
This is the default behavior of `AssemblyLoader.ResolveAssemblyAtEntryPoint()`, which can also be overridden by the user.
After the entry point is created, the loader will call `AssemblyLoader.OnAssemblyLoaded()` before returning
both the loaded assembly and the entry point to the user.

## License Info
This project is licensed under the Apache Public License 2.0.