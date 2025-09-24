using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A robust, type-safe global event manager.
/// Supports both parameterless and parameterized events.
/// </summary>
public static class EventManager
{
    // Stores parameterless events
    private static Dictionary<string, Action> eventTable = new();

    // Stores parameterized events
    private static Dictionary<string, Delegate> eventTableWithParams = new();

    #region Parameterless Events

    public static void Subscribe(string eventName, Action listener)
    {
        if (!eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] = null;
        }
        eventTable[eventName] += listener;
    }

    public static void Unsubscribe(string eventName, Action listener)
    {
        if (eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] -= listener;
        }
    }

    public static void Raise(string eventName)
    {
        if (eventTable.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent?.Invoke();
        }
    }

    #endregion

    #region Parameterized Events

    public static void Subscribe<T>(string eventName, Action<T> listener)
    {
        if (!eventTableWithParams.ContainsKey(eventName))
        {
            eventTableWithParams[eventName] = null;
        }
        eventTableWithParams[eventName] = (Action<T>)eventTableWithParams[eventName] + listener;
    }

    public static void Unsubscribe<T>(string eventName, Action<T> listener)
    {
        if (eventTableWithParams.ContainsKey(eventName))
        {
            eventTableWithParams[eventName] = (Action<T>)eventTableWithParams[eventName] - listener;
        }
    }

    public static void Raise<T>(string eventName, T arg)
    {
        if (eventTableWithParams.TryGetValue(eventName, out var thisEvent))
        {
            (thisEvent as Action<T>)?.Invoke(arg);
        }
    }

    #endregion

    #region Debug Helpers
#if UNITY_EDITOR
    public static void PrintDebug()
    {
        Debug.Log("=== EventManager Registered Events ===");
        foreach (var kvp in eventTable)
        {
            Debug.Log($"[No Params] {kvp.Key} => {kvp.Value?.GetInvocationList().Length ?? 0} listeners");
        }
        foreach (var kvp in eventTableWithParams)
        {
            Debug.Log($"[With Params] {kvp.Key} => {kvp.Value?.GetInvocationList().Length ?? 0} listeners");
        }
    }
#endif
    #endregion
}
