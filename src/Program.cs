using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Berlin.Utils;

namespace Berlin
{
    delegate void op_selection(int[] selected, int[] fitnessValues, int m);
    delegate void op_crossover(int[] child, int[] firstParent, int[] secondParent, int leftCut, int rightCut, int dataLen);

    class BestSpecimen
    {
        public int[] Nodes;
        public int Value;
    }

    class ProgramSettings
    {
        public string DataFile;
        public int PopulationSize;
        public double MutationChance;

        public op_selection Selection;
        public op_crossover Crossover;
    }

    class Program
    {
        static Random _rand = new Random();

        static bool _running = true;

        static uint _i = 0;
        static StringBuilder _outputSB = new StringBuilder();

        static void EvaluateFitness(int[,] data, int[,] pop, int m, int dataLen, int[] fitVals)
        {
            for(int i = 0;
                i < m;
                ++i)
            {
                int fitVal = 0;
                int j = 0;
                while(j < dataLen - 1)
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

        static int FindNode(int[] child, int node, int leftCut, int rightCut)
        {
            int result = -1;

            for(int i = leftCut; i <= rightCut; i++)
            {
                if(node == child[i])
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        static void PMXCrossover(int[] child, int[] firstParent, int[] secondParent, int leftCut, int rightCut, int dataLen)
        {
            for(int i = leftCut;
                i <= rightCut;
                ++i)
            {
                child[i] = firstParent[i];
            }


            for(int i = 0;
                i < leftCut;
                ++i)
            {
                int node = secondParent[i];
                int pos = -1;
                while((pos = FindNode(child, node, leftCut, rightCut)) > -1)
                {
                    node = secondParent[pos];
                }
                child[i] = node;
            }

            for(int i = rightCut + 1;
                i < dataLen;
                ++i)
            {
                int node = secondParent[i];
                int pos = -1;
                while((pos = FindNode(child, node, leftCut, rightCut)) > -1)
                {
                    node = secondParent[pos];
                }
                child[i] = node;
            }
        }

        static void OXCrossover(int[] child, int[] firstParent, int[] secondParent, int leftCut, int rightCut, int dataLen)
        {
            for(int i = leftCut;
                i <= rightCut;
                ++i)
            {
                child[i] = firstParent[i];
            }

            int nodesToCopy = dataLen - ((rightCut - leftCut) + 1);
            int childIdx = rightCut + 1;
            int parentIdx = childIdx;
            while(nodesToCopy > 0)
            {
                if(childIdx == dataLen)
                {
                    childIdx = 0;
                }
                if(parentIdx == dataLen)
                {
                    parentIdx = 0;
                }

                int node = secondParent[parentIdx];
                if(FindNode(child, node, leftCut, rightCut) > -1)
                {
                    ++parentIdx;
                }
                else
                {
                    child[childIdx] = node;
                    --nodesToCopy;
                    ++parentIdx;
                    ++childIdx;
                }
            }
        }

        static void InversionMutation(int[] child, int dataLen)
        {
            int begin = _rand.Next(dataLen / 2);
            int end = _rand.Next(dataLen / 2, dataLen);

            int len = (end - begin) + 1;
            int[] buffer = new int[len];

            int i = begin;
            int j = 0;
            while(i <= end)
            {
                buffer[j++] = child[i++];
            }

            i = end;
            j = 0;
            while(i >= begin)
            {
                child[i--] = buffer[j++];
            }
        }

        static bool Continue()
        {
            if(_i == uint.MaxValue)
            {
                return false;
            }
            else
            {
                ++_i;
                return _running;
            }
        }

        static void PrintOutput(BestSpecimen bestSpecimen, int dataLen)
        {
            _outputSB.Clear();
            for(int i = 0;
                i < dataLen;
                ++i)
            {
                int node = bestSpecimen.Nodes[i];
                _outputSB.Append(node);
                _outputSB.Append('-');
            }

            string output = string.Format("Iterations:{0} Best value:{1}\nBest path:{2}\n",
                _i,
                bestSpecimen.Value,
                _outputSB.ToString());
            Console.WriteLine(output);
        }

        static bool ParseCommandLine(string[] args, ProgramSettings settings, ref string error)
        {
            bool result = false;

            if((args != null) || (args.Length != 0))
            {
                string file = args[0];
                if(File.Exists(file))
                {
                    settings.DataFile = args[0];

                    bool popSizeParseSuccess = int.TryParse(args[1], out settings.PopulationSize);
                    if(popSizeParseSuccess)
                    {
                        /*
                            NOTE(SpectatorQL): Enables the use of a dot in run.bat,
                            instead of the culture-specific decimal point character.
                        */
                        bool mutationChanceParseSuccess = double.TryParse(args[2],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out settings.MutationChance);
                        if(mutationChanceParseSuccess)
                        {
                            for(int i = 3;
                                i < args.Length;
                                ++i)
                            {
                                string arg = args[i];
                                switch(arg)
                                {
                                    case "-tournament":
                                    {
                                        settings.Selection = TournamentSelect;
                                        break;
                                    }
                                    case "-roulette":
                                    {
                                        settings.Selection = RouletteSelect;
                                        break;
                                    }

                                    case "-PMX":
                                    {
                                        settings.Crossover = PMXCrossover;
                                        break;
                                    }
                                    case "-OX":
                                    {
                                        settings.Crossover = OXCrossover;
                                        break;
                                    }

                                    default:
                                    {
                                        Console.WriteLine("Unrecognized parameter: \"{0}\"", arg);
                                        break;
                                    }
                                }
                            }

                            if(settings.Selection != null)
                            {
                                if(settings.Crossover != null)
                                {
                                    result = true;
                                }
                                else
                                {
                                    error = "Error. Invalid crossover parameter.";
                                }
                            }
                            else
                            {
                                error = "Error. Invalid selection parameter.";
                            }
                        }
                        else
                        {
                            error = "Error. Incorrect mutation chance (expected double)";
                        }
                    }
                    else
                    {
                        error = "Error. Incorrect population size (expected int).";
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
            int dataLen;
            int[,] data;
            
            int[,] population;
            int[] fitnessValues;

#if BERLIN_DEBUG
            Debugger.Launch();
#endif
            
            Console.CancelKeyPress += (sender, e) =>
            {
                _running = false;
            };

            ProgramSettings settings = new ProgramSettings();
            string error = null;
            if(!ParseCommandLine(args, settings, ref error))
            {
                Console.WriteLine(error);
                throw new NullReferenceException();
            }

            using(FileStream stream = new FileStream(settings.DataFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using(StreamReader reader = new StreamReader(stream))
            {
                string header = reader.ReadLine();
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

#if BERLIN_DEBUG
            /*
                NOTE(SpectatorQL): This doesn't catch situations where some wild
                combination of nodes gives us the exact same sum, but at least
                it provides _some_ means of asserting that a specimen is valid.
            */
            int validationSum = 0;
            for(int i = 0;
                i < dataLen;
                ++i)
            {
                validationSum += i;
            }
#endif

            var bestSpecimen = new BestSpecimen
            {
                Nodes = new int[dataLen],
                Value = int.MaxValue
            };

            int m = settings.PopulationSize;
            double mutationChance = settings.MutationChance;
            op_selection Selection = settings.Selection;
            op_crossover Crossover = settings.Crossover;

            population = new int[m, dataLen];
            fitnessValues = new int[m];
            for(int i = 0;
                i < m;
                ++i)
            {
                for(int j = 0;
                    j < dataLen;
                    ++j)
                {
                    population[i, j] = j;
                }

                for(int j = 0;
                    j < dataLen;
                    ++j)
                {
                    int swapIdx = _rand.Next(dataLen);
                    int a = population[i, j];
                    population[i, j] = population[i, swapIdx];
                    population[i, swapIdx] = a;
                }
            }
            EvaluateFitness(data, population, m, dataLen, fitnessValues);


            int[] selected = new int[m];
            while(Continue())
            {
                Debug_StartTimer();

                int[,] newPopulation = new int[m, dataLen];
                Selection(selected, fitnessValues, m);
                
                for(int i = 0;
                    i < m;
                    i += 2)
                {
                    int[] parent1 = new int[dataLen];
                    int[] parent2 = new int[dataLen];
                    int p1 = selected[i];
                    int p2 = selected[i + 1];
                    
                    for(int j = 0;
                        j < dataLen;
                        ++j)
                    {
                        parent1[j] = population[p1, j];
                        parent2[j] = population[p2, j];
                    }


                    int midPoint = dataLen / 2;
                    int offset = 0;
                    int leftCut = _rand.Next(offset, midPoint);
                    int rightCut = _rand.Next(midPoint, dataLen - offset);


                    int[] child1 = new int[dataLen];
                    int[] child2 = new int[dataLen];
                    Crossover(child1, parent1, parent2, leftCut, rightCut, dataLen);
                    Crossover(child2, parent2, parent1, leftCut, rightCut, dataLen);


                    int range = 100;
                    double d = _rand.Next(range) / (double)range;
                    if(d <= mutationChance)
                    {
                        InversionMutation(child1, dataLen);
                    }

                    d = _rand.Next(range) / (double)range;
                    if(d <= mutationChance)
                    {
                        InversionMutation(child1, dataLen);
                    }

#if BERLIN_DEBUG
                    Debug.Assert(IsValidSpecimen(child1, validationSum));
                    Debug.Assert(IsValidSpecimen(child2, validationSum));
#endif

                    for(int j = 0;
                        j < dataLen;
                        ++j)
                    {
                        newPopulation[i, j] = child1[j];
                        newPopulation[i + 1, j] = child2[j];
                    }
                }

                population = newPopulation;
                EvaluateFitness(data, population, m, dataLen, fitnessValues);


                int bestVal = fitnessValues[0];
                int bestValIdx = 0;
                for(int i = 1;
                    i < m;
                    ++i)
                {
                    int val = fitnessValues[i];
                    if(val < bestVal)
                    {
                        bestVal = val;
                        bestValIdx = i;
                    }
                }

                if(bestVal < bestSpecimen.Value)
                {
                    for(int i = 0;
                        i < dataLen;
                        ++i)
                    {
                        bestSpecimen.Nodes[i] = population[bestValIdx, i];
                    }
                    bestSpecimen.Value = bestVal;

                    PrintOutput(bestSpecimen, dataLen);
                }


                Debug_StopTimer();
            }

            Console.WriteLine("Press any key to exit.");
        }
    }
}
