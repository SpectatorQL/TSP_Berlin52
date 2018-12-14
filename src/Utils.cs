using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Berlin
{
    class Utils
    {
        public static void Debug_PrintPopulation(int[,] pop, int len0, int len1)
        {
            for(int i = 0;
                i < len0;
                ++i)
            {
                for(int j = 0;
                    j < len1;
                    ++j)
                {
                    string printf = pop[i, j] + "   ";
                    Debug.Write(printf);
                }
                Debug.WriteLine("");
            }
        }

        public static void Debug_PrintFitnessValues(int[] fitVals)
        {
            for(int i = 0;
                i < fitVals.Length;
                ++i)
            {
                Debug.WriteLine(fitVals[i]);
            }
        }

        public static void Debug_PrintTriangle(int[,] arr, int rowLength)
        {
            for(int i = 0;
                i < rowLength;
                ++i)
            {
                for(int j = 0;
                    j <= i;
                    ++j)
                {
                    string printf = arr[i, j] + "   ";
                    Debug.Write(printf);
                }
                Debug.WriteLine("");
            }
        }

        public static void Debug_PrintSquare(int[,] arr, int rowLength)
        {
            for(int i = 0;
                i < rowLength;
                ++i)
            {
                for(int j = 0;
                    j < rowLength;
                    ++j)
                {
                    string printf = arr[i, j] + "   ";
                    Debug.Write(printf);
                }
                Debug.WriteLine("");
            }
        }
    }
}
