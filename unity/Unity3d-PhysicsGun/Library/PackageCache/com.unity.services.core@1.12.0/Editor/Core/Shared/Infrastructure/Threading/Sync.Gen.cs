// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Core.Editor.Shared.Infrastructure.Threading
{
    static class Sync
    {
        public static Task<T> RunInBackgroundThread<T>(Func<Task<T>> action)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(async() =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    var res = await action();
                    tcs.TrySetResult(res);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            });
            thread.Start();
            thread.Join();
            return tcs.Task;
        }
    }
}
