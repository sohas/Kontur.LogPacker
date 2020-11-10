using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kontur.LogPacker
{
    internal class Analyzer
    {
        //класс поиска образцов в лог-файлах. работает за линию, но имеет константную компоненту, которая заметна на малых размерах файлов

        private const int maxLineSize = 512; //ограничитель длины строки. если строка больше -- прекращаем анализ
        private const int minSizeOfSample = 6; //минимальный размер образца
        private const int samplesAmount = 20;//количество образцов, которое будет использоваться для сжатия

        internal static byte[][] Analyze(string inFileName)
        {
            //ищет в файле, разбитом на строки, образцы для класса Samples

            var fileSize = new FileInfo(inFileName).Length;

            var freq = (int)((double)fileSize / 2500 / samplesAmount + 1);
            //эмпирическая оценка периода, с которым нужно брать строки для анализа, где 2500 = 250 * 1/(1/10): 
            //250 -- длина строки, 1/10 -- сырая экономия, от которой стоит "дожимать"

            var sampless = ProcessByteLines(inFileName, freq, maxLineSize, minSizeOfSample);

            long totalAmount = sampless.Select(x => x.counter).Sum();
            var economy = totalAmount * freq;
            var part = (double)economy / fileSize;

            if (part < 0.1)
                return null;

            return sampless.Select(x => x.word).Take(samplesAmount).OrderBy(y => -y.Length).ToArray();
        }

        private static IEnumerable<SampleCounter> ProcessByteLines(string inFileName, int freq, int maxLineSize, int minSizeOfSample)
        {
            var counter = 0;
            using (var stream = File.OpenRead(inFileName))
            {
                var sampleCollector = new SampleCollector();
                var tempList = new List<byte[]>();

                foreach (var set in GetByteLinesFromStream(stream, maxLineSize))
                {
                    counter++;
                    if (counter % freq == 0)
                        tempList.Add(set);
                }

                if (tempList.Count > 2)
                    sampleCollector.Collect(GetAllCommonSubArraysInCollection(tempList, minSizeOfSample));

                return sampleCollector.GetSampleCounters();
            }
        }

        private static IEnumerable<byte[]> GetByteLinesFromStream(Stream stream, int bufferSize)
        {
            //возвращает коллекцию строк в виде байтов, разделённых '\n'. если строка длинее bufferSize, перечисление прекращается

            var buffer = new byte[bufferSize];
            var counter = 0;
            byte devider = 10;
            byte[] temp = new byte[0];
            var offset = 0;

            while (true)
            {
                if (offset > 0)
                    temp.CopyTo(buffer, 0);

                counter = stream.Read(buffer, offset, bufferSize - offset);

                if (counter == 0)
                {
                    if (temp.Length > 0 && temp.Length < bufferSize)
                        yield return temp;
                    break;
                }

                var start = 0;
                var position = 0;

                while (true)
                {
                    position = Array.IndexOf(buffer, devider, start, counter + offset - start);

                    if (position == -1)
                    {
                        temp = new byte[counter + offset - start];
                        offset = counter + offset - start;
                        Array.Copy(buffer, start, temp, 0, offset);
                        break;
                    }

                    var res = new byte[position - start + 1];
                    Array.Copy(buffer, start, res, 0, position - start + 1);
                    yield return res;
                    start = position + 1;
                }
            }
        }

        private static IEnumerable<byte[]> GetAllCommonSubArraysInCollection(IEnumerable<byte[]> text, int minLength)
        {
            //находит все повторяющиеся образцы в наборе строк

            int counter = 0;
            var collection = new List<byte[]>();
            foreach (var str in text)
            {
                if (counter++ == 0)
                {
                    collection.Add(str);
                    continue;
                }

                foreach (var collected in collection)
                {
                    foreach (var match in GetAllCommonSubArraysShiftsOff(collected, str, minLength))
                        yield return match;
                }

                collection.Add(str);
            }
        }

        private static IEnumerable<byte[]> GetAllCommonSubArraysShiftsOff(byte[] arr1, byte[] arr2, int minLength)
        {
            //для двух байтовых строк ищет без сдвига все общие подстроки длины не менее minLength

            var len = minLength > 1 ? minLength : 1;

            if (arr1.Length < len || arr2.Length < len)
                yield break;

            var max = arr1.Length < arr2.Length ? arr1.Length : arr2.Length;

            var temp = new List<byte>();

            for (var i = 0; i < max; i++)
            {
                if (arr1[i] != arr2[i])
                {
                    if (temp.Count >= len)
                        yield return temp.ToArray();
                    temp.Clear();
                }
                else
                    temp.Add(arr1[i]);
            }
            if (temp.Count >= len)
                yield return temp.ToArray();
        }
    }
}
