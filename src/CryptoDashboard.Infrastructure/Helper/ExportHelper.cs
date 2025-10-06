using System.Text;
using System.Reflection;

namespace CryptoDashboard.Infrastructure.Services
{
    public static class ExportHelper
    {
        public static byte[] ToCsv<T>(IEnumerable<T> items)
        {
            var props = typeof(T).GetProperties();
            var sb = new StringBuilder();

            // Header
            sb.AppendLine(string.Join(",", props.Select(p => p.Name)));

            // Rows
            foreach (var item in items)
            {
                var values = props.Select(p => p.GetValue(item, null)?.ToString()?.Replace(",", " ") ?? "");
                sb.AppendLine(string.Join(",", values));
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}