// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Collections.Generic;

namespace Unity.Services.Core.Editor.Shared.DependencyInversion
{
    sealed class Scope : List<IDisposable>, IDisposable
    {
        public bool Strict { get; }

        public Scope(bool strict = false)
        {
            Strict = strict;
        }

        public void Dispose()
        {
            foreach (var disposable in this)
            {
                disposable.Dispose();
            }
        }
    }
}
