using System.Collections.Generic;
using System.Text.Json;

public static class Extensions
{
    public static string Serialize<T>(this IEnumerable<T> list) => JsonSerializer.Serialize(list);
    public static List<T> Deserialize<T>(this string str) => JsonSerializer.Deserialize<List<T>>(str);
    public static List<string> Deserialize(this string str) => JsonSerializer.Deserialize<List<string>>(str);
    public static void Noop(this object value) { }
}