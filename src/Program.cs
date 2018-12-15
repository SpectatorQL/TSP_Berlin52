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
        static Flags _flags;

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

        enum Flags : byte
        {
            SELECTION_TOURNAMENT = 0x01,
            SELECTION_ROULETTE = 0x02,

            CROSSOVER_PMX = 0x04,
            CROSSOVER_OX = 0x08
        }

        static bool ParseCmdArguments(string[] args, ref string error)
        {
            bool result = false;

            if((args != null) && (args.Length != 0))
            {
                for(int i = 0;
                    i < args.Length;
                    ++i)
                {
                    string arg = args[i];
                    switch(arg)
                    {
                        case "-tournament":
                        {
                            _flags |= Flags.SELECTION_TOURNAMENT;
                            break;
                        }
                        case "-roulette":
                        {
                            _flags |= Flags.SELECTION_ROULETTE;
                            break;
                        }
                        
                        case "-PMX":
                        {
                            _flags |= Flags.CROSSOVER_PMX;
                            break;
                        }
                        case "-OX":
                        {
                            _flags |= Flags.CROSSOVER_OX;
                            break;
                        }

                        default:
                        {
                            Debug.WriteLine("Unrecognized parameter: \"{0}\"", arg);
                            break;
                        }
                    }
                }

                if(_flags != 0x00)
                {
                    result = true;
                }
                else
                {
                    error = "Error. Invalid parameters.";
                }
            }
            else
            {
                error = "Error. The program was launched without any parameters.";
            }

            return result;
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

            string error = null;
            if(!ParseCmdArguments(args, ref error))
            {
#if BERLIN_DEBUG
                _flags |= Flags.SELECTION_TOURNAMENT | Flags.CROSSOVER_OX;
#else
                Console.WriteLine(error);
                throw new NullReferenceException();
#endif
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


            while(Continue())
            {
                Debug_StartTimer();

                int[,] newPopulation = new int[M, dataLen];
                int[] selected = new int[M];

                if(_flags.HasFlag(Flags.SELECTION_TOURNAMENT))
                {
                    TournamentSelection(selected, fitnessValues, M, K);
                }
                else if(_flags.HasFlag(Flags.SELECTION_ROULETTE))
                {
                    TournamentRoulette();
                }
                else
                {
                    throw new NullReferenceException();
                }

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

                    int[] child1 = null;
                    int[] child2 = null;

                    if(_flags.HasFlag(Flags.CROSSOVER_PMX))
                    {

                    }
                    else if(_flags.HasFlag(Flags.CROSSOVER_OX))
                    {
                        child1 = OXCrossover(parent1, parent2, leftCut, rightCut, dataLen);
                        child2 = OXCrossover(parent2, parent1, leftCut, rightCut, dataLen);
                    }
                    else
                    {
                        throw new NullReferenceException();
                    }

                    // NOTE(SpectatorQL): I could probably just overwrite the original population at this point.
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

                Debug_StopTimer();
            }
            
            // NOTE(SpectatorQL): Never gets here?
            Console.ReadKey();
        }
    }
}
