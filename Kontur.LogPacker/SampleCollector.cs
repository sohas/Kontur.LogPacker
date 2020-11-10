using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.LogPacker
{
    internal class SampleCollector
    {
        //обработчик коллекции образцов

        private readonly Dictionary<int, SampleCounter> hashTable = new Dictionary<int, SampleCounter>();

        internal void Collect(byte[] word)
        {
            var sc = new SampleCounter(word);
            Collect(sc);
        }

        internal void Collect(IEnumerable<byte[]> set)
        {
            foreach (var word in set)
                Collect(word);
        }

        internal void Collect(SampleCounter sc)
        {
            var hash = sc.GetAlterHash();
            while (true)
            {
                if (hashTable.ContainsKey(hash))
                {
                    if (sc.AlterEqualsTo(hashTable[hash]))
                    {
                        hashTable[hash].counter++;
                        return;
                    }
                    hash++;
                }
                else
                {
                    hashTable[hash] = sc;
                    return;
                }
            }
        }

        internal static bool Compare(byte[] big, byte[] small)
        {
            var shift = big.Length - small.Length;
            for (var i = 0; i <= shift; i++)
            {
                var flag = true;
                for (var j = 0; j < small.Length; j++)
                {
                    flag = flag && (big[i + j] == small[j]);
                    if (!flag)
                        break;
                }
                if (flag)
                    return true;
            }
            return false;
        }

        internal IEnumerable<SampleCounter> GetSampleCounters()
        {
            var temp = hashTable.
                Select(x => new SampleCounter(x.Value.word, (int)Math.Sqrt(x.Value.counter * 2) + 1)).//восстанавливает реальное количество
                OrderBy(y => -y.word.Length).
                ToArray();

            var res = new List<SampleCounter>();

            //сокращает "дубли" образцов, отличающие друг от друга несколькими дополнительными байтами вначале или конце
            for (var i = 0; i < temp.Length; i++)
            {
                bool flag = false;

                for (var j = i + 1; j < temp.Length; j++)
                {
                    if (temp[i].word.Length == temp[j].word.Length)
                        continue;

                    if (temp[i].word.Length * 0.8 > temp[j].word.Length)
                        break;

                    if (Compare(temp[i].word, temp[j].word))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                    res.Add(temp[i]);
            }

            //в поле counter уже не количество, а экономия, которую может дать образец в анализируемом наоборе, упроядоч. по -экономии
            return res.Select(x => new SampleCounter(x.word, x.counter * (x.word.Length - 2))).OrderBy(x => -x.counter);
        }
    }
}
