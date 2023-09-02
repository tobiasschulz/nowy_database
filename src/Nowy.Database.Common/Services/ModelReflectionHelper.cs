using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nowy.Database.Common.Services;

internal sealed class ModelReflectionHelper
{
    private static readonly AtomicDictionary<Type, ModelReflectionHelper> _helper_instances = new();

    private readonly Type _instance_type;
    private readonly AtomicDictionary<string, Action<object, object?>?> _expressions_set_property = new();
    private readonly AtomicDictionary<string, Func<object, string?>?> _expressions_get_property = new();
    private string[]? _property_names = null;

    public ModelReflectionHelper(Type instance_type)
    {
        this._instance_type = instance_type;
    }

    public static ModelReflectionHelper GetHelperInstance(Type instance_type)
    {
        return _helper_instances.GetOrAddValue(instance_type, () => new ModelReflectionHelper(instance_type))!;
    }

    public void SetProperty(object instance, string name, object? value, ILogger? logger)
    {
        Action<object, object?>? expression = this._expressions_set_property.GetOrAddValue(key: name, default_value: () =>
        {
            try
            {
                return this.GetExpressionSetProperty(name: name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Failed to create expression for getting property '{name}'");
                return null;
            }
        });
        expression?.Invoke(instance, value);
    }

    public string? GetProperty(object instance, string name, ILogger? logger)
    {
        Func<object, string?>? expression = this._expressions_get_property.GetOrAddValue(key: name, default_value: () =>
        {
            try
            {
                return this.GetExpressionGetProperty(name: name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Failed to create expression for getting property '{name}'");
                return null;
            }
        });
        return expression?.Invoke(instance);
    }

    public IReadOnlyList<string> GetPropertyNames()
    {
        if (this._property_names is null)
        {
            this._property_names = this._instance_type.GetProperties()
                .Where(o => o.CanRead && o.CanWrite && o.CustomAttributes.Any(a => a.AttributeType == typeof(JsonPropertyNameAttribute)))
                .Select(o => o.Name)
                .ToArray();
        }

        return this._property_names;
    }

    public Action<object, object?>? GetExpressionSetProperty(string name)
    {
        ParameterExpression instance_param = Expression.Parameter(typeof(object));
        ParameterExpression argument_param = Expression.Parameter(typeof(object));

        PropertyInfo? property_info = this._instance_type.GetProperty(name);
        if (property_info is null)
            return null;

        Action<object, object?> expression = Expression.Lambda<Action<object, object?>>(
            Expression.Call(Expression.Convert(instance_param, this._instance_type), property_info.GetSetMethod(), Expression.Convert(argument_param, property_info.PropertyType)),
            instance_param, argument_param
        ).Compile();

        if (property_info.PropertyType == typeof(long))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = vs.ToLong();
                if (v is null)
                    v = (long)0;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(int))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = vs.ToInteger();
                if (v is null)
                    v = (int)0;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(ulong))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = vs.ToLongUnsigned();
                if (v is null)
                    v = (ulong)0;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(uint))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = vs.ToIntegerUnsigned();
                if (v is null)
                    v = (uint)0;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(double))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = vs.ToDouble();
                if (v is null)
                    v = (double)0;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(float))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = vs.ToFloat();
                if (v is null)
                    v = (float)0;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(bool))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = ( vs == "1" || vs == "true" || vs == "True" || vs == "TRUE" );
                if (v is null)
                    v = (bool)false;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(UnixTimestamp))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = UnixTimestamp.FromUnixTimeSeconds(vs.ToLong() / 1000);
                if (v is null)
                    v = UnixTimestamp.Epoch;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType == typeof(DateTimeOffset))
        {
            return (o, v) =>
            {
                if (v is string vs)
                    v = DateTimeOffset.FromUnixTimeMilliseconds(vs.ToLong());
                if (v is null)
                    v = DateTimeOffset.UnixEpoch;
                expression(o, v);
            };
        }
        else if (property_info.PropertyType.IsEnum)
        {
            return (o, v) =>
            {
                if (v is string vs)
                    Enum.TryParse(property_info.PropertyType, vs, out v);
                if (v is null)
                    v = (int)0;
                expression(o, v);
            };
        }

        return expression;
    }

    public Func<object, string?>? GetExpressionGetProperty(string name)
    {
        ParameterExpression instance_param = Expression.Parameter(typeof(object));

        PropertyInfo? property_info = this._instance_type.GetProperty(name);
        if (property_info is null)
            return null;

        Func<object, object?> expression = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(Expression.Call(Expression.Convert(instance_param, this._instance_type), property_info.GetGetMethod()), typeof(object)),
            instance_param
        ).Compile();

        if (property_info.PropertyType == typeof(long))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is long rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(int))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is int rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(ulong))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is ulong rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(uint))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is uint rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(double))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is double rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(float))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is float rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(bool))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is bool rv)
                    ret = rv.ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(UnixTimestamp))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is UnixTimestamp rv)
                    ret = ( rv.UnixTimeSeconds * 1000 ).ToString(CultureInfo.InvariantCulture);
                return ret;
            };
        }
        else if (property_info.PropertyType == typeof(DateTimeOffset))
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                if (r is DateTimeOffset rv)
                    ret = rv.ToUnixTimeMilliseconds().ToString();
                return ret;
            };
        }
        else if (property_info.PropertyType.IsEnum)
        {
            return (o) =>
            {
                object? r = expression(o);
                string? ret = null;
                if (r is string rs)
                    ret = rs;
                else
                    ret = r?.ToString();
                return ret;
            };
        }

        return o => expression(o)?.ToString();
    }
}
