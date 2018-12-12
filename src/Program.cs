using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static Berlin.Utils;

namespace Berlin
{
    class Specimen
    {
        public int[] Gene;
        public int GeneValue;

        public void CalculateGeneValue(int[,] data)
        {
            int pathSum = 0;
            int i = 0;
            while(i < Gene.Length - 1)
            {
                int row = Gene[i];
                int col = Gene[++i];
                pathSum += data[row, col];
            }
            pathSum += data[i, 0];

            GeneValue = pathSum;
        }
    }

    class Program
    {
        static Random _rand = new Random();

        const int M = 40;
        const int K = -1;

        static int[] GeneratePath(int len)
        {
            int[] result = new int[len];
            for(int i = 0; i < len; ++i)
            {
                result[i] = i;
            }

            Shuffle(result);

            return result;
        }

        static void Shuffle(int[] arr)
        {
            int len = arr.Length;
            for(int i = 0; i < len; ++i)
            {
                int swap = _rand.Next(len);

                int a = arr[i];
                arr[i] = arr[swap];
                arr[swap] = a;
            }
        }

        static bool IsDone()
        {
            // TODO: Actually do anything relevant.
            return true;
        }

        static void Main(string[] args)
        {
            string header;
            int dataLen;
            int[,] data;

#if false
            string file = "data\\berlinDebug.txt";
#else
            string file = "data\\berlin52.txt";
#endif

            using(FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using(StreamReader reader = new StreamReader(stream))
            {
                header = reader.ReadLine();
                dataLen = int.Parse(header);
                data = new int[dataLen, dataLen];

                for(int i = 0; i < dataLen; ++i)
                {
                    string[] line = reader.ReadLine()
                        .Trim()
                        .Split(' ');

                    for(int j = 0; j < i; ++j)
                    {
                        int val = int.Parse(line[j]);
                        data[i, j] = val;
                        data[j, i] = val;
                    }
                }
            }

            Specimen[] population = new Specimen[M];
            for(int i = 0; i < M; ++i)
            {
                population[i] = new Specimen();
                population[i].Gene = GeneratePath(dataLen);
                population[i].CalculateGeneValue(data);
                
                // Debug.WriteLine("specimen[{0}]: {1}", i, specimens[i].GeneValue);
            }

            while(IsDone())
            {
                /*
                    How big does this thing need to be? I've no idea.
                    Also, I don't know if this will ever need to expand.
                    Couldn't I just make an array around 2-3 times bigger
                    than M and make it work?
                */
                int count = M;
                List<Specimen> tempPopulation = new List<Specimen>(count);
            }

            // Debug_PrintSquare(data, dataLen);
            Console.ReadKey();
        }
    }
}
