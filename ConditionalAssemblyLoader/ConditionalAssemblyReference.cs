using System;

namespace ConditionalAssemblyLoader
{
    /// <summary>
    /// Defines a potential assembly and the conditions required to load it.
    /// </summary>
    public readonly struct ConditionalAssemblyReference
    {
        /// <summary>
        /// A delegate determining whether the assembly represented by <see cref="ConditionalAssemblyReference"/>
        /// should be loaded.
        /// </summary>
        public readonly Func<bool> Condition;

        /// <summary>
        /// The name of the assembly.
        /// </summary>
        public readonly string? AssemblyName;
        
        /// <summary>
        /// The path to the assembly to be loaded. Used in fallback if the assembly cannot be found using the name.
        /// </summary>
        public readonly string AssemblyFile;
        
        /// <summary>
        /// Constructs a new <see cref="ConditionalAssemblyReference"/> using the specified condition delegate
        /// and assembly path.
        /// </summary>
        /// <param name="condition">The delegate determining whether the assembly should be loaded.</param>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <param name="assemblyFile">The path to the assembly.</param>
        /// <exception cref="ArgumentException"><paramref name="assemblyName"/> and <paramref name="assemblyFile"/>
        /// are both <see langword="null"/>.</exception>
        public ConditionalAssemblyReference(Func<bool> condition, string? assemblyName, string assemblyFile)
        {
            Condition = condition;
            AssemblyName = assemblyName;
            AssemblyFile = assemblyFile;
            if (AssemblyName is null && AssemblyFile is null)
                throw new ArgumentException("Either assemblyName or assemblyFile must be specified.");
        }
    }
}