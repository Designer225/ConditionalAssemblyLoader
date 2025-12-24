using System;
using System.Collections.Generic;
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
                        Assembly? assembly = null;
                        if (e.AssemblyName != null)
                        {
                            try
                            {
                                Out?.Invoke($"[ConditionalAssemblyLoader] Attempting to load {e.AssemblyName}...");
                                assembly = Assembly.Load(new AssemblyName(e.AssemblyName));
                                Out?.Invoke(
                                    $"[ConditionalAssemblyLoader] Loaded {e.AssemblyName} from {assembly.Location}, attempting to create entry instance...");
                            }
                            catch (Exception ex)
                            {
                                Error?.Invoke(
                                    $"[ConditionalAssemblyLoader] Failed to load {e.AssemblyName} by name. Continuing by path. Exception: {ex}");
                            }
                        }

                        if (assembly is null)
                        {
                            Out?.Invoke($"[ConditionalAssemblyLoader] Attempting to load {e.AssemblyFile}...");
                            assembly = Assembly.LoadFile(e.AssemblyFile);
                            Out?.Invoke(
                                $"[ConditionalAssemblyLoader] Loaded {assembly.Location}, attempting to create entry instance...");
                        }
                        
                        var type = assembly.GetTypes().First(x => typeof(T).IsAssignableFrom(x));
                        var instance = (T)Activator.CreateInstance(type);
                        Out?.Invoke($"[ConditionalAssemblyLoader] Loaded {instance} from {assembly.Location}.");
                        OnAssemblyLoaded(instance);
                        result = new LoadedConditionalAssembly<T>(assembly, instance);
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

        /// <summary>
        /// Called when an assembly is loaded and its instance of entry type <typeparamref name="T"/> is created.
        /// </summary>
        /// <param name="value">The newly created entry instance.</param>
        protected abstract void OnAssemblyLoaded(T value);
    }
}