using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kontur.LogPacker
{
    internal class Samples
    {
        //класс образцов, которые будут заменяться их масками {marker, sampleNumber}

        internal int Length { get; } //количество образцов + 1
        internal const byte marker = 0; //маркер поиска и кодирования образцов маской {marker, sampleNumber}
        internal readonly int[] sizes00;//размеры образцов после замены marker на {marker, marker}
        internal readonly byte[] allSamplesAsBytes;//преобразованные в стрку байтов образцы без дублирования маркера и без 0го слова
        internal readonly BMSearcher[] searchers;//поисковики бойера-мура по каждому образцу00

        internal Samples(IEnumerable<byte[]> words)
        {
            var args = words.ToArray();
            if (args.Length == 0 || args.Length > 255)
                throw new ArgumentNullException("must use at least 1 and not more than 255 words");
            Length = args.Length + 1;
            sizes00 = new int[Length];
            searchers = new BMSearcher[Length];
            sizes00[0] = 1;
            searchers[0] = null;//not use in search
            allSamplesAsBytes = new ByteLiner(words).byteLine;

            for (var i = 1; i < Length; i++)
            {
                var temp = new List<byte>();
                foreach (var b in args[i - 1])
                {
                    temp.Add(b);
                    if (b == marker)
                        temp.Add(b);
                }
                sizes00[i] = temp.Count;
                searchers[i] = new BMSearcher(temp.ToArray());
            }
        }
    }
}