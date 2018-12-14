﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static Berlin.Utils;

namespace Berlin
{
    class Program
    {
        static Random _rand = new Random();

        static void EvaluateFitness(int[,] data, int[,] pop, int popLen0, int popLen1, int[] fitVals)
        {
            for(int i = 0;
                i < popLen0;
                ++i)
            {
                int fitVal = 0;
                int j = 0;
                while(j < popLen1 - 1)
                {
                    int row = pop[i, j];
                    int col = pop[i, ++j];
                    fitVal += data[row, col];
                }
                fitVal += data[j, 0];

                fitVals[i] = fitVal;
            }
        }

        static void TournamentSelection(int[] selected, int[] fitVals, int M, int K)
        {
            for(int i = 0;
                i < M;
                ++i)
            {
                int bestVal = _rand.Next(0, M);
                for(int j = 0;
                    j < K;
                    ++j)
                {
                    int next = _rand.Next(0, M);
                    if(fitVals[bestVal] < fitVals[next])
                    {
                        bestVal = next;
                    }
                }

                selected[i] = bestVal;
            }
        }

        static void TournamentRoulette()
        {
        }

        static bool IsDone()
        {
            // TODO: Actually do anything relevant.
            return true;
        }

        static void Main(string[] args)
        {
            const int M = 40;
            const int K = 3;

            string header;
            int dataLen;
            int[,] data;

            int[,] population;
            int[] fitnessValues;

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

                for(int i = 0;
                    i < dataLen;
                    ++i)
                {
                    string[] line = reader.ReadLine()
                        .Trim()
                        .Split(' ');

                    for(int j = 0;
                        j < i;
                        ++j)
                    {
                        int val = int.Parse(line[j]);
                        data[i, j] = val;
                        data[j, i] = val;
                    }
                }
            }

#if BERLIN_DEBUG
            /*
                NOTE(SpectatorQL): I would like to use QueryPerformanceCounter,
                but I'm not sure if it's worth the marshalling overhead and
                the overhead of any conversions to real time I would need to
                perform myself.
            */
            Stopwatch s = new Stopwatch();
            s.Start();
#endif

            population = new int[M, dataLen];
            fitnessValues = new int[M];
            for(int i = 0;
                i < M;
                ++i)
            {
                int j = 0;
                while(j < dataLen)
                {
                    population[i, j] = j++;
                }

                for(j = 0;
                    j < dataLen;
                    ++j)
                {
                    int swapIdx = _rand.Next(dataLen);
                    int a = population[i, j];
                    population[i, j] = population[i, swapIdx];
                    population[i, swapIdx] = a;
                }
            }
            EvaluateFitness(data, population, M, dataLen, fitnessValues);

#if BERLIN_DEBUG
            s.Stop();
            Debug.WriteLine("{0}ticks, {1}ms, {2}s",
                s.Elapsed.Ticks,
                s.ElapsedMilliseconds,
                s.ElapsedMilliseconds / (float)1000);
#endif

#if false
            Debug_PrintSquare(data, dataLen);
            Debug_PrintPopulation(population, M, dataLen);
            Debug_PrintFitnessValues(fitnessValues);
#endif

            while(IsDone())
            {
                int[,] newPopulation = new int[M, dataLen];

                int[] selected = new int[M];
                // TODO: Make separate build configurations for the two.
#if SELECTION_TOURNAMENT
                TournamentSelection(selected, fitnessValues, M, K);
#elif SELECTION_ROULETTE
                TournamentRoulette();
#else
                ASSERT_PANIC();
#endif
            }
            
            Console.ReadKey();
        }
    }
}
