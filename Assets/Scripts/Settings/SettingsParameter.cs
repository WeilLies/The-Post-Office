using System;
using System.Collections.Generic;
using UnityEngine;

public class SettingsParameter<T>
{
    public string Name { get; }
    public T Value { get; private set; }
    public T DefaultValue { get; }

    private readonly Func<T, T> _validator;
    private readonly Action<T> _onApply;

    public event Action OnChanged;

    public SettingsParameter(string name, T defaultValue, Func<T, T> validator = null, Action<T> onApply = null)
    {
        Name = name;
        DefaultValue = defaultValue;
        _validator = validator;
        _onApply = onApply;
        Value = Validate(defaultValue);
    }

    private T Validate(T raw) => _validator != null ? _validator(raw) : raw;

    public bool Set(T newValue, bool silent = false)
    {
        T validated = Validate(newValue);
        if (EqualityComparer<T>.Default.Equals(Value, validated) && !silent) return false;
        Value = validated;
        _onApply?.Invoke(Value);
        if (!silent) OnChanged?.Invoke();
        return true;
    }

    public void Reset(bool silent = false) => Set(DefaultValue, silent);

    public void ForceNotify() => OnChanged?.Invoke();
}

public class IndexedParameter
{
    public string Name { get; }
    public string[] Options { get; }
    public int Index { get; private set; }
    public string Current => Options[Index];
    public int Count => Options.Length;
    public int DefaultIndex { get; }

    private readonly Action<int> _onApply;

    public event Action OnChanged;

    public IndexedParameter(string name, string[] options, int defaultIndex = 0, Action<int> onApply = null)
    {
        Name = name;
        Options = options;
        DefaultIndex = Mathf.Clamp(defaultIndex, 0, options.Length - 1);
        _onApply = onApply;
        Index = DefaultIndex;
    }

    public bool Set(int index, bool silent = false)
    {
        int clamped = Mathf.Clamp(index, 0, Options.Length - 1);
        if (Index == clamped) return false;
        Index = clamped;
        _onApply?.Invoke(Index);
        if (!silent) OnChanged?.Invoke();
        return true;
    }

    public void Step(int direction, bool silent = false)
    {
        int next = (Index + direction + Options.Length) % Options.Length;
        Set(next, silent);
    }

    public bool SetByValue(string value, bool silent = false)
    {
        int idx = Array.IndexOf(Options, value);
        if (idx < 0) return false;
        return Set(idx, silent);
    }

    public void Reset(bool silent = false) => Set(DefaultIndex, silent);

    public void ForceNotify() => OnChanged?.Invoke();
}
