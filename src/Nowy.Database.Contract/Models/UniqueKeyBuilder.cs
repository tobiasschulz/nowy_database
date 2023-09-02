using System.Globalization;
using System.Text;

namespace Nowy.Database.Contract.Models;

public sealed class UniqueKeyBuilder
{
    private readonly StringBuilder _sb = new();

    public static UniqueKeyBuilder FromDictionary(IDictionary<string, string?> dict)
    {
        UniqueKeyBuilder builder = new();
        foreach (KeyValuePair<string, string?> pair in dict)
        {
            builder.Add(pair.Key, pair.Value);
        }

        return builder;
    }

    public static UniqueKeyBuilder FromDictionary(IDictionary<string, object?> dict)
    {
        UniqueKeyBuilder builder = new();
        foreach (KeyValuePair<string, object?> pair in dict)
        {
            builder.Add(pair.Key, Convert.ToString(pair.Value, CultureInfo.InvariantCulture));
        }

        return builder;
    }

    public void Add(string key, ReadOnlyMemory<char> value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(value);
    }

    public void Add(string key, ReadOnlySpan<char> value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(value);
    }

    public void Add(string key, string? value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(value);
    }

    public void Add(string key, long value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(string key, double value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(string key, decimal value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(string key, object? value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(key);
        this._sb.Append(": ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(ReadOnlyMemory<char> value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(value);
    }

    public void Add(ReadOnlySpan<char> value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(value);
    }

    public void Add(string? value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(value);
    }

    public void Add(long value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(double value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(decimal value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public void Add(object? value)
    {
        if (this._sb.Length != 0)
            this._sb.Append(", ");
        this._sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public override string ToString()
    {
        return this._sb.ToString();
    }


    public static implicit operator string(UniqueKeyBuilder o) => o.ToString();

}
