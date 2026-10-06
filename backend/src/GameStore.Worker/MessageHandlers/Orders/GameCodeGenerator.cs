using System.Security.Cryptography;
using System.Text;

namespace GameStore.Worker.MessageHandlers.Orders;

public class GameCodeGenerator
{
    private const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int codeLength = 12;
    private const int dashInterval = 4;

    public static List<string> GenerateCodes(int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var codes = new List<string>(count);
        using var generator = RandomNumberGenerator.Create();

        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateCode(generator));
        }

        return codes;
    }

    // Sample generated codes:
    // "A7K2-9XMQ-P4VN-8WLD"
    // "Z3H6-T1BY-R9QF-C5GJ"
    // "M8W4-K2DP-X7NL-Y0SA"
    private static string GenerateCode(RandomNumberGenerator generator)
    {
        var code = new StringBuilder(codeLength + (codeLength / dashInterval)); // Pre-allocate capacity
        var randomBytes = new byte[4];

        for (int i = 0; i < codeLength; i++)
        {
            if (i > 0 && i % dashInterval == 0)
            {
                code.Append('-');
            }

            generator.GetBytes(randomBytes);
            uint num = BitConverter.ToUInt32(randomBytes, 0);
            code.Append(characters[(int)(num % characters.Length)]);
        }

        return code.ToString();
    }
}
