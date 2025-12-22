using System.Reflection;

namespace ConditionalAssemblyLoader
{
    /// <summary>
    /// Represents a conditionally loaded assembly and its entry instance.
    /// </summary>
    /// <typeparam name="T">The type of the entry instance.</typeparam>
    public readonly struct LoadedConditionalAssembly<T>
    {
        /// <summary>
        /// The assembly that was loaded by the <see cref="AssemblyLoader{T}"/>.
        /// </summary>
        public readonly Assembly AssemblyInstance;
        
        /// <summary>
        /// The entry instance that was created by the <see cref="AssemblyLoader{T}"/>.
        /// </summary>
        public readonly T Instance;

        internal LoadedConditionalAssembly(Assembly assemblyInstance, T instance)
        {
            AssemblyInstance = assemblyInstance;
            Instance = instance;
        }
    }
}