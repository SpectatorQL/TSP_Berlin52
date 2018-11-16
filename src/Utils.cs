using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Berlin
{
    class Utils
    {
        public static void Assert(bool expr)
        {
            if(!expr)
                throw new NullReferenceException();
        }

        public static void Debug_PrintTriangle(int[,] arr, int rowLength)
        {
            for(int i = 0; i < rowLength; ++i)
            {
                for(int j = 0; j <= i; ++j)
                {
                    string printf = arr[i, j] + "   ";
                    Debug.Write(printf);
                }
                Debug.WriteLine("");
            }
        }

        public static void Debug_PrintSquare(int[,] arr, int rowLength)
        {
            for(int i = 0; i < rowLength; ++i)
            {
                for(int j = 0; j < rowLength; ++j)
                {
                    string printf = arr[i, j] + "   ";
                    Debug.Write(printf);
                }
                Debug.WriteLine("");
            }
        }
    }
}
