

using UnityEngine;

public static class MysticLog
{
    // ReSharper disable Unity.PerformanceAnalysis
    public static void Log(object message, Object context = null)
    {
#if UNITY_EDITOR
        Debug.Log(message, context);
#endif
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogWarning(object message, Object context = null)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message, context);
#endif
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogError(object message, Object context = null)
    {
#if UNITY_EDITOR
        Debug.LogError(message, context);
#endif
    }
}