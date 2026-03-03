using System.Text;
using DoctorSoft.Domain.Contracts;

namespace DoctorSoft.Data.Security;

public sealed class LegacyPasswordDecoder : ILegacyPasswordDecoder
{
    public string Decode(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return string.Empty;
        }

        var output = new StringBuilder();
        var start = 0;

        while (start < encoded.Length)
        {
            var open = encoded.IndexOf('$', start);
            if (open < 0)
            {
                break;
            }

            var close = encoded.IndexOf('$', open + 1);
            if (close < 0)
            {
                break;
            }

            var token = encoded.Substring(open + 1, close - open - 1);
            if (double.TryParse(token, out var value))
            {
                output.Append((char)Math.Round(value * 7));
            }

            start = close + 1;
        }

        return output.ToString();
    }
}
