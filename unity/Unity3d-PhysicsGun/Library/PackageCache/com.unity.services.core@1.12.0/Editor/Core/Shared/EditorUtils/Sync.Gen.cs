// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Core.Editor.Shared.EditorUtils
{
    static class Sync
    {
        public static void RunNextUpdateOnMain(
            Action action,
            [CallerFilePath] string file = null,
            [CallerMemberName] string caller = null,
            [CallerLineNumber] int line = 0)
        {
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                EditorApplication.update -= callback;
                try
                {
                    action();
                }
                catch (Exception e) when (caller != null && file != null && line != 0)
                {
                    throw new Exception($"Exception thrown from invocation made by '{file}'({line}) by {caller}", e);
                }
            };
            EditorApplication.update += callback;
        }

        public static Task SafeAsync(Func<Task> action, Action<Task> success = null)
        {
            return action().ContinueWith(t =>
            {
                RunNextUpdateOnMain(() =>
                {
                    if (t.Exception != null)
                    {
                        Debug.LogException(t.Exception);
                    }
                    else
                    {
                        success?.Invoke(t);
                    }
                });
            });
        }

        public static Task SafeAsync<T>(Func<Task<T>> action, Action<Task<T>> success = null)
        {
            return SafeAsync(
                (Func<Task>)action,
                task =>
                {
                    success?.Invoke((Task<T>)task);
                });
        }
    }
}
