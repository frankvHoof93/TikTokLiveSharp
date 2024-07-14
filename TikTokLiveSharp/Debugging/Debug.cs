using System;
using System.Runtime.CompilerServices;

namespace TikTokLiveSharp.Debugging
{
    /// <summary>
    /// Simple Logging-Wrapper for TikTokLiveSharp
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Logs Message to Console
        /// </summary>
        /// <param name="message">Message to Log</param>
        /// <param name="context">Context for Message</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY
        [UnityEngine.HideInCallstack]
#endif
        public static void Log(string message,
#if UNITY
            UnityEngine.Object context = null)
        {
            UnityEngine.Debug.Log($"[TikTokClient] {message}", context);
        }
#else
            object context = null)
        {
            Console.WriteLine(message);
            if (context != null)
                Console.WriteLine(context.ToString());
        }
#endif

        /// <summary>
        /// Logs Warning to Console
        /// </summary>
        /// <param name="message">Warning to Log</param>
        /// <param name="context">Context for Warning</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY
        [UnityEngine.HideInCallstack]
#endif
        public static void LogWarning(string message,
#if UNITY
            UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogWarning($"[TikTokClient] {message}", context);
        }
#else
            object context = null)
        {
            Console.WriteLine($"[WARNING] {message}");
            if (context != null)
                Console.WriteLine(context.ToString());
        }
#endif

        /// <summary>
        /// Logs Error to Console
        /// </summary>
        /// <param name="message">Error to Log</param>
        /// <param name="context">Context for Error</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY
        [UnityEngine.HideInCallstack]
#endif
        public static void LogError(string message,
#if UNITY
            UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError($"[TikTokClient] {message}", context);
        }
#else
            object context = null)
        {
            Console.WriteLine($"[ERROR] {message}");
            if (context != null)
                Console.WriteLine(context.ToString());
        }
#endif

        /// <summary>
        /// Logs Exception to Console
        /// </summary>
        /// <param name="message">Exception to Log</param>
        /// <param name="context">Context for Exception</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY
        [UnityEngine.HideInCallstack]
#endif
        public static void LogException(Exception e,
#if UNITY
            UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogException(e, context);
        }
#else
            object context = null)
        {
            Console.WriteLine($"[ERROR] {e.ToString()}");
            if (context != null)
                Console.WriteLine(context.ToString());
        }
#endif
    }
}

