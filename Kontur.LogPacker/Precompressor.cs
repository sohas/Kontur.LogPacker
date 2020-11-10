using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Kontur.LogPacker
{
    internal class Precompressor
    {
        //класс замены образцов в файле на их маски {marker, sampleNumber} 

        private const int precompressBufferSize = 16384;

        internal static void Compress(string inFileName, string outFileName, Samples samples)
        {
            using (var inStream = File.OpenRead(inFileName))
            using (var outStream = File.OpenWrite(outFileName))
            using (var gzipStream = new GZipStream(outStream, CompressionLevel.Optimal, true))
            {
                var header = new Header().MakePrecompressHeader(samples.allSamplesAsBytes);
                gzipStream.Write(header, 0, header.Length);
                Precompress(inStream, gzipStream, samples);
            }
        }

        internal static void Compress0(string inFileName, string outFileName)
        {
            //стандартное сжатие без обработки

            using (var inStream = File.OpenRead(inFileName))
            using (var outStream = File.OpenWrite(outFileName))
            using (var gzipStream = new GZipStream(outStream, CompressionLevel.Optimal, true))
                inStream.CopyTo(gzipStream);
        }

        private static void Precompress(Stream inStream, Stream outStream, Samples samples)
        {
            //заменяет в потоке образцы на их маски {marker, sampleNumber}

            var precompressBufferSize = 16384;
            var buffer = new byte[precompressBufferSize * 2];
            int counter = 0;
            byte marker = Samples.marker;

            while (true)
            {
                counter = inStream.Read(buffer, 0, precompressBufferSize);
                if (counter == 0) break;

                for (var i = 0; i < samples.Length; i++)
                {
                    var finder = i == 0 ? FindMarkerPositions(buffer, marker, counter) : samples.searchers[i].Search(buffer, 0, counter);
                    counter = ReplaceSample(ref buffer, counter, marker, finder, samples.sizes00[i], (byte)i);
                }

                outStream.Write(buffer, 0, counter);
            }
        }

        private static IEnumerable<int> FindMarkerPositions(byte[] array, byte marker, int counter)
        {
            //находит все позиции маркера в массиве в пределах области считывания

            var start = 0;

            while (true)
            {
                start = Array.IndexOf(array, marker, start, counter - start);
                if (start == -1)
                    yield break;
                yield return start++;
            }
        }

        private static int ReplaceSample(ref byte[] buffer, int counter, byte marker, IEnumerable<int> finder, int sampleLen, byte sampleNumber)
        {
            //в передаваемом по ref массиве в пределах области считывания заменяет все вхождения образца маской {marker, sampleNumber} 
            //и возвращает изменённую область считывания. адреса всех вхождений возвращает передаваемый в метод finder.
            //предполагается, что область считывания передаваемого массива - не более половины его размера. это необходимо для
            //худщего случая, когда все байты в области считывания равны marker.

            var target = new byte[buffer.Length];
            var sourceIndex = 0;
            var destinIndex = 0;
            var shift = 0;

            foreach (int samplePosition in finder)
            {
                shift = samplePosition - sourceIndex;
                Array.Copy(buffer, sourceIndex, target, destinIndex, shift);
                destinIndex += shift;
                target[destinIndex++] = marker;
                target[destinIndex++] = sampleNumber;
                sourceIndex = samplePosition + sampleLen;
            }

            Array.Copy(buffer, sourceIndex, target, destinIndex, counter - sourceIndex);
            counter += destinIndex - sourceIndex;
            buffer = target;
            return counter;
        }
    }
}
