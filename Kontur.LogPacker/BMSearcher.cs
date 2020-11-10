using System;
using System.Collections.Generic;

namespace Kontur.LogPacker
{
    internal class BMSearcher 
    {
        //поиск Boyer-Moore в массиве байтов

        private readonly byte[] sample;
        private readonly int[] chars;
        private readonly int[] offsets;

        internal BMSearcher(byte[] sample)
        {
            this.sample = sample;
            chars = MakeChars(sample);
            offsets = MakeOffsets(sample);
        }

        internal IEnumerable<int> Search(byte[] array, int start, int end)
        {
            if (start < 0 || start > array.Length || end < 0 || end > array.Length || start > end)
                throw new ArgumentOutOfRangeException("start and end  must be > 0 and <= array.Length, start must be <= end");

            if (sample.Length == 0)
                yield break;

            for (int i = start + sample.Length - 1; i < end;)
            {
                int j;

                for (j = sample.Length - 1; sample[j] == array[i]; i--, j--)
                {
                    if (j != 0)
                        continue;

                    yield return i;

                    i += sample.Length - 1;
                    break;
                }

                i += offsets[sample.Length - 1 - j] > chars[array[i]] ? offsets[sample.Length - 1 - j] : chars[array[i]];
            }
        }

        private static int[] MakeChars(byte[] sample)
        {
            int[] chars = new int[256];

            for (int i = 0; i < 256; i++)
                chars[i] = sample.Length;

            for (int i = 0; i < sample.Length - 1; i++)
                chars[sample[i]] = sample.Length - 1 - i;

            return chars;
        }

        private static int[] MakeOffsets(byte[] sample)
        {
            int[] offsets = new int[sample.Length];
            int lastPrefixPosition = sample.Length;

            for (int i = sample.Length - 1; i >= 0; i--)
            {
                if (IsPrefix(sample, i + 1))
                    lastPrefixPosition = i + 1;

                offsets[sample.Length - 1 - i] = lastPrefixPosition - i + sample.Length - 1;
            }

            for (int i = 0; i < sample.Length - 1; ++i)
            {
                int suffLen = GetSuffixLength(sample, i);
                offsets[suffLen] = sample.Length - 1 - i + suffLen;
            }

            return offsets;
        }

        private static bool IsPrefix(byte[] sample, int p)
        {
            for (int i = p, j = 0; i < sample.Length; i++, j++)
                if (sample[i] != sample[j])
                    return false;

            return true;
        }

        private static int GetSuffixLength(byte[] sample, int p)
        {
            int len = 0;

            for (int i = p, j = sample.Length - 1; i >= 0 && sample[i] == sample[j]; i--, j--)
                len++;

            return len;
        }
    }
}