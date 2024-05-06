using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.ObjectPools.Internal
{
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReturnToAnotherPool()
        {
            throw new ArgumentException("Attempting to return to another pool.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void IsCanTakeIsFalse()
        {
            throw new InvalidOperationException("The pool is empty and cannot create a new instance.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InvalidPoolID()
        {
            throw new ArgumentException("Invalid pool id.");
        }


    }
}
