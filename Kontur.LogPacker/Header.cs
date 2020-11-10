using System;

namespace Kontur.LogPacker
{
    internal class Header
    {
        //класс заголовка для файла, обработанного проектом

        internal readonly byte[] headerKey = new byte[] { 17, 255, 37, 143 };

        internal byte[] MakePrecompressHeader(byte[] byteLine)
        {
            var res = new byte[4 + 4 + byteLine.Length + 4];
            headerKey.CopyTo(res, 0);
            headerKey.CopyTo(res, 4 + 4 + byteLine.Length);
            byteLine.CopyTo(res, 4 + 4);
            BitConverter.GetBytes(4 + 4 + byteLine.Length).CopyTo(res, 4);
            return res;
        }
    }
}