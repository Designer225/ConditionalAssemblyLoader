using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConditionalAssemblyLoader
{
    /// <summary>
    /// Defines an assembly loader that loads the proper assembly depending on the conditions fulfilled.
    /// Upon loading, the loader creates a new instance of <typeparamref name="T"/> that serves as the entry point.
    /// </summary>
    /// <typeparam name="T">The type of the entry instance.</typeparam>
    /// <remarks>
    /// <list type="number">
    /// <item>When attempting to load an assembly, the <see cref="AssemblyLoader{T}"/> will traverse the <see cref="References"/>
    /// list from the first to the last reference entry, stopping ahead of time if it finds an entry that fulfills all of its conditions.</item>
    /// <item>Next, the loader will load the entry's assembly and then create an instance of <typeparamref name="T"/>,
    /// which serves as the entry point to the assembly.</item>
    /// <item>Finally, the loader will call <see cref="OnAssemblyLoaded"/> with the newly created instance and
    /// returns the instance and assembly to the user.</item>
    /// </list>
    /// <para>
    /// The <see cref="References"/> list can be modified by the user before a load attempt is made.
    /// Both <see cref="Out"/> and <see cref="Error"/> can be modified to use alternative outputs.
    /// </para>
    /// </remarks>
    public abstract class AssemblyLoader<T> where T : class
    {
        /// <summary>
        /// A list of conditional assembly references.
        /// Update this to specify which assembly to use depending on conditions.
        /// </summary>
        // ReSharper disable once CollectionNeverUpdated.Global
        public readonly List<ConditionalAssemblyReference> References = new List<ConditionalAssemblyReference>();
        
        private Action<string>? _out = Console.Out.WriteLine;
        private Action<string>? _error = Console.Error.WriteLine;

        /// <summary>
        /// The delegate for writing out messages. Uses <see cref="Console.Out"/> by default.
        /// Setting this to <see langword="null"/> disables it.
        /// </summary>
        public Action<string>? Out
        {
            get => _out;
            set => _out = value;
        }

        /// <summary>
        /// The delegate for writing error messages. Uses <see cref="Console.Error"/> by default.
        /// Setting this to <see langword="null"/> disables it.
        /// </summary>
        public Action<string>? Error
        {
            get => _error;
            set => _error = value;
        }

        /// <summary>
        /// Attempts to load the assembly that fulfills the conditions specified by the runtime environment.
        /// </summary>
        /// <param name="result">If <see langword="true"/>,
        /// returns an entry indicating the loaded assembly and an instance of the entry type.
        /// Otherwise, this value is undefined.</param>
        /// <param name="error">If <see langword="true"/>, returns <see langword="null"/>.
        /// Otherwise, returns the error that aborted the load.</param>
        /// <returns><see langword="true"/> if an assembly was loaded; otherwise, <see langword="false"/>.</returns>
        public bool TryLoad(out LoadedConditionalAssembly<T> result, out Exception? error)
        {
            foreach (var e in References)
            {
                Out?.Invoke($"[ConditionalAssemblyLoader] Checking conditions for {e.AssemblyFile}...");
                if (e.Condition())
                {
                    try
                    {
                        var assembly = LoadAssembly(e);
                        result = CreateEntryPoint(assembly);
                        error = null;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke($"[ConditionalAssemblyLoader] Exception: {ex}");
                        result = default;
                        error = ex;
                        return false;
                    }
                }
            }

            result = default;
            error = new InvalidOperationException("No assembly available fulfills the requisite conditions.");
            return false;
        }

        private Assembly LoadAssembly(in ConditionalAssemblyReference reference)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyAtLoad;
                
                Assembly? assembly = null;
                if (reference.AssemblyName != null)
                {
                    try
                    {
                        Out?.Invoke($"[ConditionalAssemblyLoader] Attempting to load {reference.AssemblyName}...");
                        assembly = Assembly.Load(new AssemblyName(reference.AssemblyName));
                        Out?.Invoke(
                            $"[ConditionalAssemblyLoader] Loaded {reference.AssemblyName} from {assembly.Location}, attempting to create entry instance...");
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(
                            $"[ConditionalAssemblyLoader] Failed to load {reference.AssemblyName} by name. Continuing by path. Exception: {ex}");
                    }
                }

                if (assembly is null)
                {
                    Out?.Invoke($"[ConditionalAssemblyLoader] Attempting to load {reference.AssemblyFile}...");
                    assembly = Assembly.LoadFile(reference.AssemblyFile);
                    Out?.Invoke(
                        $"[ConditionalAssemblyLoader] Loaded {assembly.Location}, attempting to create entry instance...");
                }

                return assembly;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssemblyAtLoad;
            }
        }

        /// <summary>
        /// Called when an assembly is being loaded and the default resolution fails.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event data.</param>
        /// <returns>The assembly that resolves the type, assembly, or resource;
        /// or <see langword="null"/> if the assembly cannot be resolved.</returns>
        protected virtual Assembly? ResolveAssemblyAtLoad(object sender, ResolveEventArgs args)
        {
            // attempts to load assembly if it's located in the same directory as the interface assembly
            var directory = Path.GetDirectoryName(GetType().Assembly.Location);
            Out?.Invoke($"[ConditionalAssemblyLoader] Seeking matching assembly for {args.Name} in {directory}...");
            if (directory is null) return null;
            var path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFile(path) : null;
        }

        private LoadedConditionalAssembly<T> CreateEntryPoint(Assembly assembly)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyAtEntryPoint;
                var type = assembly.GetTypes().First(x => typeof(T).IsAssignableFrom(x));
                var instance = (T)Activator.CreateInstance(type);
                Out?.Invoke($"[ConditionalAssemblyLoader] Loaded {instance} from {assembly.Location}.");
                OnAssemblyLoaded(instance);
                return new LoadedConditionalAssembly<T>(assembly, instance);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssemblyAtEntryPoint;
            }
        }

        /// <summary>
        /// Called when an entry point is being loaded and the default resolution of a dependency assembly fails.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event data.</param>
        /// <returns>The assembly that resolves the type, assembly, or resource;
        /// or <see langword="null"/> if the assembly cannot be resolved.</returns>
        protected virtual Assembly? ResolveAssemblyAtEntryPoint(object sender, ResolveEventArgs args)
        {
            // attempts to select one of the loaded assemblies
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var result = loadedAssemblies.FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
            if (result != null)
                Out?.Invoke($"[ConditionalAssemblyLoader] Resolved {args.Name} with {result} (from {result.Location}).");
            return result;
        }

        /// <summary>
        /// Called when an assembly is loaded and its instance of entry type <typeparamref name="T"/> is created.
        /// </summary>
        /// <param name="value">The newly created entry instance.</param>
        protected abstract void OnAssemblyLoaded(T value);
    }
}