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

        static void TournamentSelect(int[] selected, int[] fitVals, int M, int K)
        {
            for(int i = 0;
                i < M;
                ++i)
            {
                int bestSpecimen = _rand.Next(0, M);
                for(int j = 0;
                    j < K;
                    ++j)
                {
                    int nextSpecimen = _rand.Next(0, M);
                    if(fitVals[nextSpecimen] < fitVals[bestSpecimen])
                    {
                        bestSpecimen = nextSpecimen;
                    }
                }

                selected[i] = bestSpecimen;
            }
        }

        static void RouletteSelect(int[] selected, int[] fitVals, int M)
        {
            // NOTE(SpectatorQL): Everything is inverted because best == lowest.
            const double CHANCE = 0.5f;
            int fitnessSum = ArraySum(fitVals, M);
            for(int i = 0;
                i < M;
                ++i)
            {
                int j = 0;
                double jChance = _rand.NextDouble();
                do
                {
                    jChance -= fitVals[j++] / (double)fitnessSum;
                } while(jChance > CHANCE);

                selected[i] = j;
            }
        }

        static int[] PMXCrossover(int[] p1, int[] p2, int leftCut, int rightCut, int dataLen)
        {
            int[] child = new int[dataLen];
            
            for(int i = leftCut;
                i <= rightCut;
                ++i)
            {
                child[i] = p1[i];
            }

            /*
                NOTE(SpectatorQL): Instead of doing all the evil stuff with (index, value)
                pairs right away, I figured I could just give the child all of p2's remaining nodes
                first and then do all those crazy shenanigans.
            */
            for(int i = 0;
                i < dataLen;
                ++i)
            {
                if(i == leftCut)
                {
                    i = rightCut + 1;
                }

                child[i] = p2[i];
            }
            
            List<int> freeIndices = FreeIndices(p1, p2, leftCut, rightCut);
            
            /*
                NOTE(SpectatorQL): Please, for the love of God, don't ever ask
                me how this thing works or why it's written the way it is.
                It drove me FREAKING NUTS.
                This part especially.
                While I get the general idea of how PMX works, trying to
                translate the algorithm into code was just PURE EVIL.
            */
            for(int i = 0;
                i < freeIndices.Count;
                ++i)
            {
                int freeIndex = freeIndices[i];
                int nodeToCopy = p2[freeIndex];
                
                int copyIndex = freeIndex;
                do
                {
                    int searchedNode = p1[copyIndex];
                    for(int j = 0;
                        j < dataLen;
                        ++j)
                    {
                        if(p2[j] == searchedNode)
                        {
                            copyIndex = j;
                            break;
                        }
                    }
                } while(IndexInsideCrossSection(copyIndex, leftCut, rightCut));
                
                child[copyIndex] = nodeToCopy;
            }

            return child;
        }

        static List<int> FreeIndices(int[] p1, int[] p2, int leftCut, int rightCut)
        {
            List<int> indices = new List<int>();
            
            for(int i = leftCut;
                i <= rightCut;
                ++i)
            {
                int node = p2[i];
                bool notInCrossSection = true;

                for(int j = leftCut;
                    j <= rightCut;
                    ++j)
                {
                    if(node == p1[j])
                    {
                        notInCrossSection = false;
                        break;
                    }
                }

                if(notInCrossSection)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        static bool IndexInsideCrossSection(int idx, int start, int end)
        {
            bool result = (idx >= start && idx <= end) ? true : false;
            return result;
        }

        static int[] OXCrossover(int[] p1, int[] p2, int leftCut, int rightCut, int dataLen)
        {
            int[] child = new int[dataLen];

            int crossLen = (rightCut - leftCut) + 1;
            // TODO: stackalloc
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

        static void Mutate(int[] child, int dataLen)
        {
            // TODO: stackalloc
            const int len = 3;
            int[] nodesToMutate = new int[len]
            {
                _rand.Next(dataLen),
                _rand.Next(dataLen),
                _rand.Next(dataLen)
            };
            
            for(int i = 0;
                i < len;
                ++i)
            {
                int j = _rand.Next(dataLen);
                int k = nodesToMutate[i];

                int node = child[j];
                child[j] = child[k];
                child[k] = node;
            }
        }

        static bool Continue(int[,] pop, int[] fitVals, int M, int dataLen)
        {
#if true
            if(_i % 1000 == 0)
            {
                int bestVal = fitVals[0];
                int bestValIdx = 0;
                for(int i = 1;
                    i < M;
                    ++i)
                {
                    int val = fitVals[i];
                    if(val < bestVal)
                    {
                        bestVal = val;
                        bestValIdx = i;
                    }
                }

                // TODO: Cache to reduce GC overhead.
                System.Text.StringBuilder sb = new System.Text.StringBuilder(dataLen * 3);
                for(int i = 0;
                    i < dataLen;
                    ++i)
                {
                    int node = pop[bestValIdx, i];
                    sb.Append(node);
                    sb.Append(' ');
                }

                string output = string.Format("Iterations:{0}  Best value:{1}\nBest path:{2}\n",
                    _i,
                    bestVal,
                    sb.ToString());
                Console.WriteLine(output);
            }
#endif

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
            SelectionMask = 0b00000011,
            CrossoverMask = 0b00001100,

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
                
                // NOTE(SpectatorQL): If only C# would let me define implicit conversions on enums... Oh well.
                byte selectionMask = (byte)Flags.SelectionMask;
                byte crossoverMask = (byte)Flags.CrossoverMask;
                if(((byte)_flags != 0x00)
                    && (((byte)_flags & selectionMask) != selectionMask)
                    && (((byte)_flags & selectionMask) != 0x00)
                    && (((byte)_flags & crossoverMask) != crossoverMask)
                    && (((byte)_flags & crossoverMask) != 0x00))
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
            const double MUTATION_CHANCE = 0.04;

            string header;
            int dataLen;
            int[,] data;

            /*
                NOTE(SpectatorQL): Stackalloc may be a good option, but I'm not sure
                whether using it would incur any constraints on passing the
                population as an argument to functions.
            */
            int[,] population;
            int[] fitnessValues;

#if BERLIN_DEBUG
            _flags |= Flags.SELECTION_TOURNAMENT | Flags.CROSSOVER_PMX;
#else
            string error = null;
            if(!ParseCmdArguments(args, ref error))
            {
                Console.WriteLine(error);
                throw new NullReferenceException();
            }
#endif

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


            while(Continue(population, fitnessValues, M, dataLen))
            {
                Debug_StartTimer();

                int[,] newPopulation = new int[M, dataLen];
                int[] selected = new int[M];

                if(_flags.HasFlag(Flags.SELECTION_TOURNAMENT))
                {
                    TournamentSelect(selected, fitnessValues, M, K);
                }
                else if(_flags.HasFlag(Flags.SELECTION_ROULETTE))
                {
                    /*
                        TODO: Sort the population.
                        NOTE(SpectatorQL): In order for RouletteSelect to work the
                        population needs to be sorted based on the fitness values.
                        Which sucks, because I end up calling EvaluateFitness twice.
                        But if I don't do that then the algorithm gets wild.
                    */
                    RouletteSelect(selected, fitnessValues, M);
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

                    // TODO: Multithreading
                    if(_flags.HasFlag(Flags.CROSSOVER_PMX))
                    {
                        child1 = PMXCrossover(parent1, parent2, leftCut, rightCut, dataLen);
                        child2 = PMXCrossover(parent2, parent1, leftCut, rightCut, dataLen);
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

                    // TODO: Multithreading
                    double divisor = 1000.0;
                    double d = _rand.Next(11) / divisor;
                    if(d > MUTATION_CHANCE)
                    {
                        Mutate(child1, dataLen);
                    }

                    d = _rand.Next(101) / divisor;
                    if(d > MUTATION_CHANCE)
                    {
                        Mutate(child2, dataLen);
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
