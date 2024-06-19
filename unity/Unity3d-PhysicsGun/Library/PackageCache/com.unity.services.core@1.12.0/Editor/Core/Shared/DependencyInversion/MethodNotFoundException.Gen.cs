// WARNING: Auto generated code. Modifications will be lost!
using System;

namespace Unity.Services.Core.Editor.Shared.DependencyInversion
{
    class MethodNotFoundException : Exception
    {
        public MethodNotFoundException(Type type, string methodName)
            : base($"Type {type.Name} must have a single public method called {methodName}")
        {
        }
    }
}
