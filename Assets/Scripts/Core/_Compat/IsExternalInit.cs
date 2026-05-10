// Compiler-required marker for `init` setters on records (used by NoteEvent, etc.).
// The C# 9 compiler synthesizes references to System.Runtime.CompilerServices.IsExternalInit
// for any `init` setter, but Unity's netstandard2.1 BCL doesn't define it. Defining a stub
// here is the standard workaround — purely a compile-time marker, never used at runtime.

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
