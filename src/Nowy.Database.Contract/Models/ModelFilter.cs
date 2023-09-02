using System.Globalization;
using System.Text.Json.Serialization;

namespace Nowy.Database.Contract.Models;

public enum ModelFilterOperator
{
    NONE = 0,
    EQUAL = 1,
    LESS = 2,
    GREATER = 3,
    LESS_OR_EQUAL = 4,
    GREATER_OR_EQUAL = 5,
    IN = 6,
    EXIST = 7,
}

public enum ModelFilterDataType
{
    STRING = 0,
    BOOL = 1,
    LONG = 2,
    DOUBLE = 3,
    DATETIMEOFFSET = 4,
}

public sealed class ModelFilterBuilder
{
    public List<ModelFilterBuilder>? FiltersAnd { get; private init; }
    public List<ModelFilterBuilder>? FiltersOr { get; private init; }
    public ModelFilterBuilder? FilterNot { get; private init; }

    public ModelFilterOperator? Operator { get; private init; }
    public string? Property { get; private init; }
    public string? Value { get; private init; }
    public List<string>? Values { get; private init; }
    public ModelFilterDataType DataType { get; private init; }


    public static ModelFilterBuilder And(params ModelFilterBuilder[] filter_builders)
    {
        return new ModelFilterBuilder
        {
            FiltersAnd = filter_builders.ToList(),
        };
    }

    public static ModelFilterBuilder Or(params ModelFilterBuilder[] filter_builders)
    {
        return new ModelFilterBuilder
        {
            FiltersOr = filter_builders.ToList(),
        };
    }

    public static ModelFilterBuilder Not(ModelFilterBuilder filter_builder)
    {
        return new ModelFilterBuilder
        {
            FilterNot = filter_builder,
        };
    }

    public static ModelFilterBuilder Equals(string property, string value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.EQUAL,
            Property = property,
            Value = value,
        };
    }

    public static ModelFilterBuilder Equals(string property, long value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.EQUAL,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.LONG,
        };
    }

    public static ModelFilterBuilder Equals(string property, double value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.EQUAL,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.DOUBLE,
        };
    }

    public static ModelFilterBuilder Equals(string property, bool value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.EQUAL,
            Property = property,
            Value = ( value ? 1 : 0 ).ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.BOOL,
        };
    }

    public static ModelFilterBuilder Equals(string property, DateTimeOffset value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.EQUAL,
            Property = property,
            Value = value.ToString("o"),
            DataType = ModelFilterDataType.DATETIMEOFFSET,
        };
    }

    public static ModelFilterBuilder Less(string property, string value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS,
            Property = property,
            Value = value,
        };
    }

    public static ModelFilterBuilder Less(string property, long value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.LONG,
        };
    }

    public static ModelFilterBuilder Less(string property, double value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.DOUBLE,
        };
    }

    public static ModelFilterBuilder Less(string property, DateTimeOffset value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS,
            Property = property,
            Value = value.ToString("o"),
            DataType = ModelFilterDataType.DATETIMEOFFSET,
        };
    }

    public static ModelFilterBuilder Greater(string property, string value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER,
            Property = property,
            Value = value,
        };
    }

    public static ModelFilterBuilder Greater(string property, long value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.LONG,
        };
    }

    public static ModelFilterBuilder Greater(string property, double value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.DOUBLE,
        };
    }

    public static ModelFilterBuilder Greater(string property, DateTimeOffset value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER,
            Property = property,
            Value = value.ToString("o"),
            DataType = ModelFilterDataType.DATETIMEOFFSET,
        };
    }

    public static ModelFilterBuilder LessOrEqual(string property, string value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS_OR_EQUAL,
            Property = property,
            Value = value,
        };
    }

    public static ModelFilterBuilder LessOrEqual(string property, long value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS_OR_EQUAL,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.LONG,
        };
    }

    public static ModelFilterBuilder LessOrEqual(string property, double value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS_OR_EQUAL,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.DOUBLE,
        };
    }

    public static ModelFilterBuilder LessOrEqual(string property, DateTimeOffset value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.LESS_OR_EQUAL,
            Property = property,
            Value = value.ToString("o"),
            DataType = ModelFilterDataType.DATETIMEOFFSET,
        };
    }

    public static ModelFilterBuilder GreaterOrEqual(string property, string value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER_OR_EQUAL,
            Property = property,
            Value = value,
        };
    }

    public static ModelFilterBuilder GreaterOrEqual(string property, long value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER_OR_EQUAL,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.LONG,
        };
    }

    public static ModelFilterBuilder GreaterOrEqual(string property, double value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER_OR_EQUAL,
            Property = property,
            Value = value.ToString(CultureInfo.InvariantCulture),
            DataType = ModelFilterDataType.DOUBLE,
        };
    }

    public static ModelFilterBuilder GreaterOrEqual(string property, DateTimeOffset value)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.GREATER_OR_EQUAL,
            Property = property,
            Value = value.ToString("o"),
            DataType = ModelFilterDataType.DATETIMEOFFSET,
        };
    }

    public static ModelFilterBuilder In(string property, IEnumerable<string> values)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.IN,
            Property = property,
            Values = values.ToList(),
        };
    }

    public static ModelFilterBuilder In(string property, IEnumerable<long> values)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.IN,
            Property = property,
            Values = values.Select(o => o.ToString(CultureInfo.InvariantCulture)).ToList(),
        };
    }

    public static ModelFilterBuilder Exists(string property)
    {
        return new ModelFilterBuilder
        {
            Operator = ModelFilterOperator.EXIST,
            Property = property,
        };
    }

    public ModelFilter Build()
    {
        if (FiltersAnd is { })
        {
            return new ModelFilter { FiltersAnd = FiltersAnd.Select(o => o.Build()).ToList(), };
        }

        if (FiltersOr is { })
        {
            return new ModelFilter { FiltersOr = FiltersOr.Select(o => o.Build()).ToList(), };
        }

        if (FilterNot is { })
        {
            return new ModelFilter { FilterNot = FilterNot.Build(), };
        }

        return new ModelFilter { Operator = Operator, Property = Property, Value = Value, Values = Values, DataType = DataType, };
    }
}

public class ModelFilter
{
    [JsonPropertyName("and")] public List<ModelFilter>? FiltersAnd { get; set; }
    [JsonPropertyName("or")] public List<ModelFilter>? FiltersOr { get; set; }
    [JsonPropertyName("not")] public ModelFilter? FilterNot { get; set; }

    [JsonPropertyName("operator")] public ModelFilterOperator? Operator { get; set; }
    [JsonPropertyName("property")] public string? Property { get; set; }
    [JsonPropertyName("value")] public string? Value { get; set; }
    [JsonPropertyName("values")] public List<string>? Values { get; set; }
    [JsonPropertyName("type")] public ModelFilterDataType DataType { get; set; }
}
