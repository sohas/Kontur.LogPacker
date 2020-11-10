using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.LogPacker
{
    internal class ByteLiner
    {
        //преобразователь массива образцов в строку

        private readonly byte[] devideKey = new byte[] { 239, 1, 219, 113 };
        internal readonly byte[][] bytess;
        internal readonly byte[] byteLine;
        internal readonly bool success;

        internal ByteLiner(IEnumerable<byte[]> bytess)
        {
            this.bytess = bytess.ToArray();
            var res = new List<byte>();

            foreach (var bytes in bytess)
            {
                res.AddRange(devideKey);
                res.AddRange(BitConverter.GetBytes(res.Count + 4 + bytes.Length));
                res.AddRange(bytes);
            }

            byteLine = res.ToArray();
            success = true;
        }

        internal ByteLiner(byte[] byteLine)
        {
            this.byteLine = byteLine;
            var res = new List<byte[]>();
            int temp = 0;

            var finder = new BMSearcher(devideKey).Search(byteLine, 0, byteLine.Length);

            foreach (var position in finder)
            {
                if (position != temp)
                    continue;

                temp = BitConverter.ToInt32(byteLine, position + 4);
                var bytes = new byte[temp - position - 8];
                Array.Copy(byteLine, position + 8, bytes, 0, temp - position - 8);
                res.Add(bytes);
            }

            if (temp != byteLine.Length)
            {
                success = false;
                bytess = null;
            }
            else
                success = true;

            bytess = res.ToArray();
        }
    }
}
