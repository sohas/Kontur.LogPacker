using System;

namespace Kontur.LogPacker
{
    internal class SampleCounter
    {
        //счётчик образцов

        internal readonly byte[] word;
        internal int counter;

        internal SampleCounter(byte[] word)
        {
            this.word = word;
            counter = 1;
        }

        internal SampleCounter(byte[] word, int amount)
        {
            this.word = word;
            counter = amount;
        }

        internal bool AlterEqualsTo(SampleCounter sc2)
        {
            return CompareArrays(word, 0, sc2.word);
        }

        internal int GetAlterHash()
        {
            var res = 0;
            foreach (var e in word)
                res = res * 257 + e;
            return res;
        }

        private static bool CompareArrays(byte[] array, int offset, byte[] sample)
        {
            //проверяет совпадает ли подмассив с образцом

            if (offset < 0 || offset + sample.Length > array.Length)
                return false;

            for (var i = 0; i < sample.Length; i++)
            {
                if (sample[i] != array[i + offset])
                    return false;
            }

            return true;
        }
    }
}
