// WARNING: Auto generated code. Modifications will be lost!
using System;

namespace Unity.Services.Core.Editor.Shared.DependencyInversion
{
    class ConstructorNotFoundException : Exception
    {
        public ConstructorNotFoundException(Type type)
            : base($"Type {type.Name} must have a single public or internal constructor.")
        {
        }
    }
}
