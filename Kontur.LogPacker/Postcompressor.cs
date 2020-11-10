using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Kontur.LogPacker
{
    internal class Postcompressor
    {
        //класс высстановлентя образцов в сжатом файле по их маскам {marker, sampleNumber}

        private const int postcompressBufferSize = 16384;

        internal static void Decompress(string inFileName, string outFileName)
        {
            using (var inStream = File.OpenRead(inFileName))
            using (var outStream = File.OpenWrite(outFileName))
            using (var gzipStream = new GZipStream(inStream, CompressionMode.Decompress, true))
                Postcompress(gzipStream, outStream);
        }

        private static void Postcompress(Stream inStream, Stream outStream)
        {
            //при наличии "правильного" заголовка, восстанавливает из него образцы и в потоке 
            //заменяет маски образцов {marker, sampleNumber} на образцы. иначе -- отправляет поток без изменений

            var buffer = new byte[postcompressBufferSize];
            int counter = 0;
            counter = inStream.Read(buffer, 0, 8);

            //проверка заголовка
            var checkAdress = BitConverter.ToInt32(buffer, 4);
            var headerKey = new Header().headerKey;
            if (counter != 8 || !CompareArrays(buffer, 0, headerKey) || checkAdress < 9 || checkAdress > postcompressBufferSize - 5)
            {
                outStream.Write(buffer, 0, counter);
                inStream.CopyTo(outStream, postcompressBufferSize);
                return;
            }
            counter = inStream.Read(buffer, 8, checkAdress - 8 + 4);
            if (counter != checkAdress - 8 + 4 || !CompareArrays(buffer, checkAdress, headerKey))
            {
                outStream.Write(buffer, 8, counter);
                inStream.CopyTo(outStream, postcompressBufferSize);
                return;
            }
            var allSamlesAsBytes = new byte[checkAdress - 8];
            Array.Copy(buffer, 8, allSamlesAsBytes, 0, checkAdress - 8);

            //построение образцов для замен
            var resamples = new ReSamples(allSamlesAsBytes);
            if (!resamples.success)
            {
                outStream.Write(buffer, 8, counter);
                inStream.CopyTo(outStream, postcompressBufferSize);
                return;
            }

            //замены масок на образцы
            counter = 0;
            var marker = ReSamples.marker;
            var transp = false;
            while (true)
            {
                //подготовка массива buffer: перенос при нечётном числе масок подряд в конце области считывания последнюю маску в следующую итерацию
                if (transp)
                {
                    buffer[0] = marker;
                    counter = inStream.Read(buffer, 1, postcompressBufferSize - 1) + 1;
                }
                else
                    counter = inStream.Read(buffer, 0, postcompressBufferSize);
                if (counter == 0)
                    break;
                transp = AreOddMarkersAtEnd(buffer, counter, marker);
                if (transp)
                    counter--;
                if (counter == 0)
                    throw new FormatException("wrong format of zipped file");

                //собственно замены масок
                var finder = FindMarkerPositions(buffer, marker, counter);
                RestoreSamplesAndWriteToStream(buffer, counter, outStream, finder, resamples);
            }
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

        private static bool AreOddMarkersAtEnd(byte[] buffer, int counter, byte marker)
        {
            //проверяет, есть ли в конце области считывания массива подряд идущее нечётное число байтов, равных marker
            //это необходимо для того, чтобы предотвратить разрыв маски {marker, sampleNumber} при её считывании из буфера.

            var res = false;

            for (var i = counter - 1; i >= 0; i--)
            {
                if (buffer[i] != marker)
                    break;
                else
                    res = !res;
            }

            return res;
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

        private static void RestoreSamplesAndWriteToStream(byte[] buffer, int counter, Stream outStream, IEnumerable<int> finder, ReSamples resamples)
        {
            //принимает массив с масками {marker, markerNumber}, восстанавливает образцы по маскам, заменяет в массиве маски на образцы 
            //и пишет массив в передаваемый ему поток.

            var bufferSize = buffer.Length;
            var tempBuffer = new byte[bufferSize];
            var position = 0;
            var sourceIndex = 0;
            var targetIndex = 0;
            var shift = 0;
            var finderEnum = finder.GetEnumerator();
            var marker = ReSamples.marker;

            while (finderEnum.MoveNext())
            {
                position = finderEnum.Current;
                shift = position - sourceIndex;

                if (targetIndex + shift > bufferSize)
                {
                    outStream.Write(tempBuffer, 0, targetIndex);
                    targetIndex = 0;
                }

                if (shift > 0)
                {
                    Array.Copy(buffer, sourceIndex, tempBuffer, targetIndex, shift);
                    sourceIndex += shift;
                    targetIndex += shift;
                }

                var sampleNumber = buffer[++position];

                if (sampleNumber >= resamples.Length)
                    throw new FormatException("wrong format");

                if (sampleNumber == marker)
                    finderEnum.MoveNext();

                shift = resamples.sizes0[sampleNumber];

                if (targetIndex + shift > bufferSize)
                {
                    outStream.Write(tempBuffer, 0, targetIndex);
                    targetIndex = 0;
                }

                Array.Copy(resamples.words0[sampleNumber], 0, tempBuffer, targetIndex, shift);
                sourceIndex += 2;
                targetIndex += shift;
            }

            outStream.Write(tempBuffer, 0, targetIndex);
            outStream.Write(buffer, sourceIndex, counter - sourceIndex);
        }
    }
}
