using System;
using Lodino.Core.Errors;

namespace Lodino.Core.Safety
{
    public static class SecurityLimits
    {
        public const long DefaultMaxDataFileSize = 100 * 1024 * 1024; // 100 MB
        public const int MaxJsonNestingDepth = 128;
        public const int MaxCsvColumns = 10000;
        public const int MaxCsvRows = 1000000;
        public const int MaxIniSections = 5000;
        public const int MaxIniKeysPerSection = 10000;

        public static void AssertFileSize(long length, long maxBytes = DefaultMaxDataFileSize)
        {
            if (length > maxBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "O arquivo ({0:N0} bytes) excede o limite máximo de segurança permitido de {1:N0} bytes.",
                    length, maxBytes));
            }
        }

        public static void AssertJsonDepth(int currentDepth)
        {
            if (currentDepth > MaxJsonNestingDepth)
            {
                throw new ResourceLimitException(string.Format(
                    "Profundidade da estrutura JSON ({0}) ultrapassa o teto máximo permitido de {1} níveis.",
                    currentDepth, MaxJsonNestingDepth));
            }
        }
    }
}
