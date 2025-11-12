using System;
using System.Text.Json.Nodes;

using Extractor.Exceptions;

namespace Extractor.Utilities
{
    internal static class JsonNodeExtensions
    {
        internal static T GetOneOf<T>(this JsonNode node, string[] properties) where T : JsonNode
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var index = 0;
            try
            {
                for (index = 0; index < properties.Length; index++)
                {
                    if (node[properties[index]] != null) return (T)node[properties[index]];
                }
            }
            catch (InvalidCastException)
            {
                throw new ParsingException($"Wrong data type at {properties[index]}");
            }

            throw new ParsingException($"Unable to get any of the properties {string.Join(",", properties)}");
        }

        internal static bool TryGetOneOf<T>(this JsonNode node, string[] properties, out T result) where T : JsonNode
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            result = null;
            try
            {
                foreach (var property in properties)
                {
                    if (node[property] != null && node[property] is T val)
                    {
                        result = val;
                        return true;
                    };
                }
            }
            catch (Exception) { }

            return false;
        }

        internal static T Get<T>(this JsonNode node, string path)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));

            try
            {
                return node.GotoPath(path).GetValue<T>();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
            {
                throw new ParsingException($"Wrong data type at {path}");
            }
        }

        internal static bool TryGet<T>(this JsonNode node, string path, out T result)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));

            result = default;
            try
            {
                result = node.GotoPath(path).GetValue<T>();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static T Get<T>(this JsonArray node, int index)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            try
            {
                return node[index].GetValue<T>();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
            {
                throw new ParsingException($"Wrong data type at {index}");
            }
        }

        internal static bool TryGet<T>(this JsonArray node, int index, out T result)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            result = default;
            try
            {
                result = node[index].GetValue<T>();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static JsonObject GetObject(this JsonNode node, string path)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));

            try
            {
                return node.GotoPath(path).AsObject();
            }
            catch (InvalidOperationException)
            {
                throw new ParsingException($"Wrong data type at {path}");
            }
        }

        internal static bool TryGetObject(this JsonNode node, string path, out JsonObject result)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));

            result = null;
            try
            {
                result = node.GotoPath(path).AsObject();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static JsonObject GetObject(this JsonArray node, int index)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            try
            {
                return node[index].AsObject();
            }
            catch (InvalidOperationException)
            {
                throw new ParsingException($"Wrong data type at index {index}");
            }
        }
        
        internal static bool TryGetObject(this JsonArray node, int index, out JsonObject result)
        {
	        if (node == null) throw new ArgumentNullException(nameof(node));
	        
	        result = null;
	        try
	        {
		        result = node[index].AsObject();
		        return true;
	        }
	        catch (Exception)
	        {
		        return false;
	        }
        }

        internal static bool Has(this JsonNode node, string path)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));
            
            return node.GotoPath(path.Split('.').AsSpan(), out _) != null;
        }

        internal static JsonArray GetArray(this JsonNode node, string path)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));

            try
            {
                return node.GotoPath(path).AsArray();
            }
            catch (InvalidOperationException)
            {
                throw new ParsingException($"Wrong data type at {path}");
            }
        }

        internal static bool TryGetArray(this JsonNode node, string path, out JsonArray result)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (path == null) throw new ArgumentNullException(nameof(path));

            result = null;
            try
            {
                result = node.GotoPath(path).AsArray();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static JsonArray GetArray(this JsonArray node, int index)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            try
            {
                return node[index].AsArray();
            }
            catch (InvalidOperationException)
            {
                throw new ParsingException($"Wrong data type at index {index}");
            }
        }

        private static JsonNode GotoPath(this JsonNode node, string path)
        {
            var keys = path.Split('.');
            return GotoPath(node, keys.AsSpan(), out var missingKey) ?? throw new ParsingException($"Unable to get {missingKey} in path {path}");
        }

        private static JsonNode GotoPath(this JsonNode node, ReadOnlySpan<string> keys, out string missingKey)
        {
            var result = node;
            missingKey = null;
            foreach (var key in keys)
            {
                result = result[key];
                if (result == null)
                {
                    missingKey = key;
                    break;
                }
            }

            return result;
        }
    }
}