using System.Collections.Generic;
using System.Linq;

namespace Kontur.LogPacker
{
    internal class ReSamples
    {
        //класс образцов, которые будут восстанавливаться по их маскам {marker, sampleNumber}

        internal int Length { get; } //количество образцов + 1
        internal const byte marker = 0; //маркер поиска и кодирования образцов маской {marker, sampleNumber}
        internal readonly byte[][] words0; //образцы, преобразованные в последовательности байтов (без дублирования маркера)
        internal readonly int[] sizes0; //размеры образцов-байт последовательностей
        internal readonly bool success; //индикатор успешного восстановления образцов по заголовку файла

        internal ReSamples(byte[] allSamplesAsBytes)
        {
            var res = new List<byte[]>() { new byte[] { 0 } };
            var byteLiner = new ByteLiner(allSamplesAsBytes);
            res.AddRange(byteLiner.bytess);
            words0 = res.ToArray();
            success = byteLiner.success;
            Length = words0.Length;
            sizes0 = words0.Select(x => x.Length).ToArray();
        }
    }
}