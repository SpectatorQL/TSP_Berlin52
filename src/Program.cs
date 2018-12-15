using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static Berlin.Utils;

namespace Berlin
{
    class Program
    {
        static Random _rand = new Random();
        static uint _i = 0;

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

        static int[] OXCrossover(int[] p1, int[] p2, int leftCut, int rightCut, int dataLen)
        {
            int[] child = new int[dataLen];

            int crossLen = (rightCut - leftCut) + 1;
            int[] crossSection = new int[crossLen];
            
            for(int i = leftCut;
                i <= rightCut;
                ++i)
            {
                child[i] = p1[i];
            }

            Array.Copy(child, leftCut, crossSection, 0, crossLen);
            Array.Sort(crossSection);
            
            int nodesToCopy = dataLen - crossLen;
            {
                int i = rightCut + 1;
                int j = i;
                while(nodesToCopy > 0)
                {
                    if(i >= dataLen)
                    {
                        i = 0;
                    }
                    if(j >= dataLen)
                    {
                        j = 0;
                    }

                    int node = p2[j];
                    if(!NodeIsInCrossSection(node, crossSection))
                    {
                        child[i] = node;
                        --nodesToCopy;
                        ++j;
                        ++i;
                    }
                    else
                    {
                        ++j;
                    }
                }
            }

            return child;
        }

        /*
            TODO: Binary search!
            NOTE(SpectatorQL): At the moment this thing is pretty fast, but
            binary search will probably make it even faster. We'll see.
        */
        static bool NodeIsInCrossSection(int node, int[] crossSection)
        {
            bool result = false;

            for(int i = 0;
                i < crossSection.Length;
                ++i)
            {
                if(crossSection[i] == node)
                {
                    result = true;
                    return result;
                }
            }

            return result;
        }

        static bool Continue()
        {
            if(_i++ < uint.MaxValue)
            {
                return true;
            }
            else
            {
                return false;
            }
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

            foreach(string arg in args)
            {
                Console.WriteLine(arg);
            }

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

            while(Continue())
            {
#if BERLIN_DEBUG
                s.Restart();
#endif

                int[,] newPopulation = new int[M, dataLen];

                int[] selected = new int[M];
                // TODO: Make separate build configurations for the two.
                // TODO: Or maybe invoke them based on a cmd argument?
#if SELECTION_TOURNAMENT
                TournamentSelection(selected, fitnessValues, M, K);
#elif SELECTION_ROULETTE
                TournamentRoulette();
#else
                ASSERT_PANIC();
#endif
                int i = 0;
                while(i < M)
                {
                    int[] parent1 = new int[dataLen];
                    int[] parent2 = new int[dataLen];
                    int p1 = i;
                    int p2 = p1 + 1;

                    for(int j = 0;
                        j < dataLen;
                        ++j)
                    {
                        parent1[j] = population[selected[p1], j];
                        parent2[j] = population[selected[p2], j];
                    }

                    int leftCut = -1;
                    int rightCut = -1;
                    // NOTE(SpectatorQL): I could probably use dataLen to calculate this. We'll see.
#if false
                    int dist = 1;
#else
                    int dist = 10;
#endif
                    while(dist > (rightCut - leftCut))
                    {
                        int midPoint = dataLen / 2;
                        leftCut = _rand.Next(1, midPoint);
                        rightCut = _rand.Next(midPoint, dataLen - 1);
                    }

                    // OX Crossover
                    int[] child1 = OXCrossover(parent1, parent2, leftCut, rightCut, dataLen);
                    int[] child2 = OXCrossover(parent2, parent1, leftCut, rightCut, dataLen);

                    for(int j = 0;
                        j < dataLen;
                        ++j)
                    {
                        newPopulation[p1, j] = child1[j];
                        newPopulation[p2, j] = child2[j];
                    }

                    i += 2;
                }

                population = newPopulation;
                EvaluateFitness(data, population, M, dataLen, fitnessValues);

#if BERLIN_DEBUG
                s.Stop();
                Debug.WriteLine("{0}ticks, {1}ms, {2}s",
                    s.Elapsed.Ticks,
                    s.ElapsedMilliseconds,
                    s.ElapsedMilliseconds / (float)1000);
#endif
            }
            
            // NOTE(SpectatorQL): Never gets here?
            Console.ReadKey();
        }
    }
}
