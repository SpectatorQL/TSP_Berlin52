using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Berlin.Utils;

namespace Berlin
{
    class Program
    {
        static Random _rand = new Random();

        static Flags _flags;
        static string _file;
        static int _m;
        const double MUTATION_CHANCE = 0.04;

        static uint _i = 0;
        static ulong _mutations = 0;
        static StringBuilder _outputSB = new StringBuilder();

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

        static void TournamentSelect(int[] selected, int[] fitVals, int m)
        {
            const int K = 3;
            for(int i = 0;
                i < m;
                ++i)
            {
                int bestSpecimen = _rand.Next(0, m);
                for(int j = 0;
                    j < K;
                    ++j)
                {
                    int nextSpecimen = _rand.Next(0, m);
                    if(fitVals[nextSpecimen] < fitVals[bestSpecimen])
                    {
                        bestSpecimen = nextSpecimen;
                    }
                }

                selected[i] = bestSpecimen;
            }
        }

        static void RouletteSelect(int[] selected, int[] fitVals, int m)
        {
            int fitnessSum = ArraySum(fitVals, m);
            for(int i = 0;
                i < m;
                ++i)
            {
                int j = 0;
                double sum = 0;
                double jChance = _rand.NextDouble();
                do
                {
                    sum += fitVals[j] / (double)fitnessSum;
                } while(jChance > sum);

                selected[i] = j;
            }
        }

        static void PMXCrossover(int[]child, int[] p1, int[] p2, int leftCut, int rightCut, int dataLen)
        {
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

        static void OXCrossover(int[] child, int[] p1, int[] p2, int leftCut, int rightCut, int dataLen)
        {
            for(int i = leftCut;
                i <= rightCut;
                ++i)
            {
                child[i] = p1[i];
            }

            int crossLen = (rightCut - leftCut) + 1;
            int[] crossSection = new int[crossLen];
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
                    if(NodeOutsideCrossSection(node, crossSection))
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
        }

        static bool NodeOutsideCrossSection(int node, int[] crossSection)
        {
            bool result = false;

            int bsResult = Array.BinarySearch(crossSection, node);
            if(bsResult < 0)
            {
                result = true;
            }

            return result;
        }

        static void Mutate(int[] child, int dataLen)
        {
            const int LEN = 3;
            int[] nodesToMutate = new int[LEN]
            {
                _rand.Next(dataLen),
                _rand.Next(dataLen),
                _rand.Next(dataLen)
            };
            
            for(int i = 0;
                i < LEN;
                ++i)
            {
                int j = _rand.Next(dataLen);
                int k = nodesToMutate[i];

                int node = child[j];
                child[j] = child[k];
                child[k] = node;
            }
        }

        static bool Continue(int[,] pop, int[] fitVals, int m, int dataLen)
        {
            if(_i % 1000 == 0)
            {
                int bestVal = fitVals[0];
                int bestValIdx = 0;
                for(int i = 1;
                    i < m;
                    ++i)
                {
                    int val = fitVals[i];
                    if(val < bestVal)
                    {
                        bestVal = val;
                        bestValIdx = i;
                    }
                }
                
                _outputSB.Clear();
                for(int i = 0;
                    i < dataLen;
                    ++i)
                {
                    int node = pop[bestValIdx, i];
                    _outputSB.Append(node);
                    _outputSB.Append('-');
                }

                string output = string.Format("Iterations:{0} Mutations:{1} Best value:{2}\nBest path:{3}\n",
                    _i,
                    _mutations,
                    bestVal,
                    _outputSB.ToString());
                Console.WriteLine(output);
            }

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
            CROSSOVER_OX = 0x08,

            // NOTE(SpectatorQL): 0b00000011
            SelectionMask = SELECTION_TOURNAMENT | SELECTION_ROULETTE,
            // NOTE(SpectatorQL): 0b00001100
            CrossoverMask = CROSSOVER_PMX | CROSSOVER_OX,
        }

        static bool ParseCmdArguments(string[] args, ref string error)
        {
            bool result = false;

            if((args != null) || (args.Length != 0))
            {
                string file = args[0];
                if(File.Exists(file))
                {
                    _file = args[0];

                    bool parseSuccess = int.TryParse(args[1], out _m);
                    if(parseSuccess)
                    {
                        for(int i = 2;
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
                                    Console.WriteLine("Unrecognized parameter: \"{0}\"", arg);
                                    break;
                                }
                            }
                        }

                        byte selectionMask = (byte)Flags.SelectionMask;
                        byte crossoverMask = (byte)Flags.CrossoverMask;
                        byte selectionFlags = (byte)((byte)_flags & selectionMask);
                        byte crossoverFlags = (byte)((byte)_flags & crossoverMask);

                        if(((byte)_flags != 0)
                            && ((selectionFlags & (selectionFlags - 1)) == 0)
                            && (selectionFlags != 0)

                            && ((crossoverFlags & (crossoverFlags - 1)) == 0)
                            && (crossoverFlags != 0))
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
                        error = "Error. Incorrect population size.";
                    }
                }
                else
                {
                    error = "Error. File " + file + " not found.";
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
            string header;
            int dataLen;
            int[,] data;
            
            int[,] population;
            int[] fitnessValues;


#if BERLIN_DEBUG
            _flags |= Flags.SELECTION_TOURNAMENT | Flags.CROSSOVER_OX;
            _file = "data\\berlin52.txt";
            _m = 40;
#else
            string error = null;
            if(!ParseCmdArguments(args, ref error))
            {
                Console.WriteLine(error);
                throw new NullReferenceException();
            }
#endif

            using(FileStream stream = new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                        j <= i;
                        ++j)
                    {
                        int val = int.Parse(line[j]);
                        data[i, j] = val;
                        data[j, i] = val;
                    }
                }
            }

            population = new int[_m, dataLen];
            fitnessValues = new int[_m];
            for(int i = 0;
                i < _m;
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
            EvaluateFitness(data, population, _m, dataLen, fitnessValues);


            int[] selected = new int[_m];
            while(Continue(population, fitnessValues, _m, dataLen))
            {
                Debug_StartTimer();

                if(_flags.HasFlag(Flags.SELECTION_TOURNAMENT))
                {
                    TournamentSelect(selected, fitnessValues, _m);
                }
                else if(_flags.HasFlag(Flags.SELECTION_ROULETTE))
                {
                    RouletteSelect(selected, fitnessValues, _m);
                }
                else
                {
                    throw new NullReferenceException();
                }
                
                for(int i = 0;
                    i < _m;
                    i += 2)
                {
                    // NOTE(SpectatorQL): Use pointers instead of copying?
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
                    int minLen = 10;
                    while(minLen > (rightCut - leftCut))
                    {
                        int midPoint = dataLen / 2;
                        leftCut = _rand.Next(1, midPoint);
                        rightCut = _rand.Next(midPoint, dataLen - 1);
                    }


                    int[] child1 = new int[dataLen];
                    int[] child2 = new int[dataLen];
                    
                    if(_flags.HasFlag(Flags.CROSSOVER_PMX))
                    {
                        PMXCrossover(child1, parent1, parent2, leftCut, rightCut, dataLen);
                        PMXCrossover(child2, parent2, parent1, leftCut, rightCut, dataLen);
                    }
                    else if(_flags.HasFlag(Flags.CROSSOVER_OX))
                    {
                        OXCrossover(child1, parent1, parent2, leftCut, rightCut, dataLen);
                        OXCrossover(child2, parent2, parent1, leftCut, rightCut, dataLen);
                    }
                    else
                    {
                        throw new NullReferenceException();
                    }
                    

                    int range = 1000;
                    double d = _rand.Next(range) / (double)range;
                    if(MUTATION_CHANCE >= d)
                    {
                        ++_mutations;
                        Mutate(child1, dataLen);
                    }

                    d = _rand.Next(range) / (double)range;
                    if(MUTATION_CHANCE >= d)
                    {
                        ++_mutations;
                        Mutate(child2, dataLen);
                    }

                    
                    for(int j = 0;
                        j < dataLen;
                        ++j)
                    {
                        population[p1, j] = child1[j];
                        population[p2, j] = child2[j];
                    }
                }
                
                EvaluateFitness(data, population, _m, dataLen, fitnessValues);
                
                Debug_StopTimer();
            }
            
            // NOTE(SpectatorQL): Never gets here?
            Console.ReadKey();
        }
    }
}
