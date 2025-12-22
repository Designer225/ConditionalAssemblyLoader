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
        /// The path to the assembly to be loaded.
        /// </summary>
        public readonly string AssemblyFile;
        
        /// <summary>
        /// Constructs a new <see cref="ConditionalAssemblyReference"/> using the specified condition delegate
        /// and assembly path.
        /// </summary>
        /// <param name="condition">The delegate determining whether the assembly should be loaded.</param>
        /// <param name="assemblyFile">The path to the assembly.</param>
        public ConditionalAssemblyReference(Func<bool> condition, string assemblyFile)
        {
            Condition = condition;
            AssemblyFile = assemblyFile;
        }
    }
}