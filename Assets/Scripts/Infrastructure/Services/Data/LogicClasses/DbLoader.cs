using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Cysharp.Threading.Tasks;
using MasterMemory;
using MasterMemory.Meta;
using UnityEngine;


namespace SpiralingStudio.Services.DataManagement
{
   public interface IDbLoaderFactory
{
    IDbLoader Create(CSVTableReferences references);
}

public sealed class DbLoaderFactory : IDbLoaderFactory
{
    public IDbLoader Create(CSVTableReferences references)
    {
        return new DbLoader(references);
    }
}

public interface IDbLoader : IDisposable
{
    //MemoryDatabase Database { get; }
}

public sealed class DbLoader : IDbLoader
{
    private bool _disposed;
    //MemoryDatabase _db;

    //public MemoryDatabase Database => _db;

    public DbLoader(CSVTableReferences references)
    {
        if (references == null)
        {
            //throw new ArgumentNullException(nameof(references), "CSV table references cannot be null.");
        }
        //_db = BuildDatabase(references);
    }

    // MemoryDatabase BuildDatabase(CSVTableReferences references)
    // {
    //     var meta = MemoryDatabase.GetMetaDatabase();
    //     var builder = new DatabaseBuilder();
    //     foreach (var kvp in references.TableNameToCsvReference)
    //     {
    //         var tableName = kvp.Key;
    //         var textAsset = kvp.Value?.editorAsset as TextAsset;
    //         if (!textAsset) continue;
    //
    //         var metaTable = meta.GetTableInfo(tableName);
    //         if (metaTable == null) continue;
    //
    //         var rows = ParseCsv(textAsset.text, metaTable);
    //         builder.AppendDynamic(metaTable.DataType, rows);
    //     }
    //     return new MemoryDatabase(builder.Build());
    // }

    List<object> ParseCsv(string csvText, MetaTable metaTable)
    {
        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogWarning($"[DbLoader] Empty CSV text provided for table '{metaTable?.TableName ?? "unknown"}'.");
            return new List<object>();
        }

        if (metaTable == null)
        {
            throw new ArgumentNullException(nameof(metaTable), "MetaTable cannot be null.");
        }

        var results = new List<object>();
        try
        {
            using var sr = new StringReader(csvText);
            using var csv = new CsvReader(sr, new CsvConfiguration(CultureInfo.InvariantCulture));
            csv.Read();
            csv.ReadHeader();
            
            int rowIndex = 0;
            while (csv.Read())
            {
                try
                {
                    rowIndex++;
                    var data = Activator.CreateInstance(metaTable.DataType);
                    
                    foreach (var prop in metaTable.Properties)
                    {
                        try
                        {
                            var raw = csv.GetField(prop.Name);
                            if (raw == null)
                            {
                                continue;
                            }
                            var parsed = ParseValue(prop.PropertyInfo.PropertyType, raw);
                            prop.PropertyInfo.SetValue(data, parsed);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[DbLoader] Failed to parse property '{prop.Name}' at row {rowIndex} in table '{metaTable?.TableName}': {ex.Message}");
                        }
                    }
                    results.Add(data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DbLoader] Failed to parse row {rowIndex} in table '{metaTable.TableName}': {ex}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DbLoader] Critical error parsing CSV for table '{metaTable.TableName}': {ex}");
            throw;
        }
        
        return results;
    }
    
    
    static object ParseValue(Type type, string rawValue)
{
    // Trim to handle whitespace-only cells.
    rawValue = rawValue?.Trim();

    // If string type, return as-is (including possibly empty).
    if (type == typeof(string))
    {
        return rawValue;
    }

    // If blank/empty, supply defaults for certain types.
    if (string.IsNullOrEmpty(rawValue))
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // e.g. int?, bool?, enum? => null
            return null;
        }
        if (type.IsEnum)
        {
            // e.g. no valid enum => default enum value (0)
            return Activator.CreateInstance(type);
        }
        // Value types: return typed default, reference types: null
        if (type == typeof(DateTime)) return DateTime.MinValue;
        if (type == typeof(Guid)) return Guid.Empty;
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    // Handle nullable<T> with non-empty rawValue
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
    {
        return ParseValue(type.GenericTypeArguments[0], rawValue);
    }

    // Handle enum by name or integer
    if (type.IsEnum)
    {
        if (Enum.TryParse(type, rawValue, ignoreCase: true, out var resultByName))
        {
            return resultByName;
        }
        else if (int.TryParse(rawValue, out int enumInt))
        {
            return Enum.ToObject(type, enumInt);
        }
        throw new FormatException($"Cannot parse enum {type.FullName} from '{rawValue}'.");
    }

    // Handle other built-in types
    switch (Type.GetTypeCode(type))
    {
        case TypeCode.Boolean:
            // 'TRUE', 'FALSE', '0', '1'
            var lower = rawValue.ToLowerInvariant();
            if (lower == "true") return true;
            if (lower == "false") return false;
            if (lower == "0") return false;
            if (lower == "1") return true;
            return bool.Parse(rawValue); // fallback

        case TypeCode.Char:
            return char.Parse(rawValue);
        case TypeCode.SByte:
            return sbyte.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Byte:
            return byte.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Int16:
            return short.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.UInt16:
            return ushort.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Int32:
            return int.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.UInt32:
            return uint.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Int64:
            return long.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.UInt64:
            return ulong.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Single:
            return float.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Double:
            return double.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.Decimal:
            return decimal.Parse(rawValue, CultureInfo.InvariantCulture);
        case TypeCode.DateTime:
            return DateTime.Parse(rawValue, CultureInfo.InvariantCulture);

        default:
            if (type == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(rawValue, CultureInfo.InvariantCulture);
            }
            if (type == typeof(TimeSpan))
            {
                return TimeSpan.Parse(rawValue, CultureInfo.InvariantCulture);
            }
            if (type == typeof(Guid))
            {
                return Guid.Parse(rawValue);
            }
            throw new NotSupportedException($"Unsupported property type: {type.FullName}");
    }
}

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // _db = null;
    }
}


}