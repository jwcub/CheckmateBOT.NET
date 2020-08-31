﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckmateBOT.NET
{
    static class ToolKit
    {
        public static bool ArrEq(int[] a, int[] b)
        {
            if (a.Length == b.Length)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        //b in a
        public static bool ListContainsArr(List<int[]> a, int[] b)
        {
            foreach (var tmp in a)
            {
                if (ArrEq(tmp, b))
                {
                    return true;
                }
            }
            return false;
        }

        public static void ListRemoveArr(ref List<int[]> a, int[] b)
        {
            for (int i = 0; i < a.Count; i++)
            {
                if (ArrEq(a[i], b))
                {
                    a.RemoveAt(i);
                }
            }
        }
    }
}
