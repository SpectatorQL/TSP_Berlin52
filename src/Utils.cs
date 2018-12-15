using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Berlin
{
    class Utils
    {
        /*
            NOTE(SpectatorQL): I would like to use QueryPerformanceCounter,
            but I'm not sure if it's worth the marshalling overhead and
            the overhead of any conversions to real time I would need to
            perform myself.
        */
        static Stopwatch _s = new Stopwatch();

        public static int ArraySum(int[] arr, int len)
        {
            int result = arr[0];

            for(int i = 1;
                i < len;
                ++i)
            {
                result += arr[i];
            }

            return result;
        }

        [Conditional("BERLIN_DEBUG")]
        public static void Debug_StartTimer()
        {
            _s.Restart();
        }

        [Conditional("BERLIN_DEBUG")]
        public static void Debug_StopTimer()
        {
            _s.Stop();
            Debug.WriteLine("{0}ticks, {1}ms, {2}s",
                _s.Elapsed.Ticks,
                _s.ElapsedMilliseconds,
                _s.ElapsedMilliseconds / (float)1000);
        }

        [Conditional("BERLIN_DEBUG")]
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

        [Conditional("BERLIN_DEBUG")]
        public static void Debug_PrintFitnessValues(int[] fitVals)
        {
            for(int i = 0;
                i < fitVals.Length;
                ++i)
            {
                Debug.WriteLine(fitVals[i]);
            }
        }

        [Conditional("BERLIN_DEBUG")]
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

        [Conditional("BERLIN_DEBUG")]
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
