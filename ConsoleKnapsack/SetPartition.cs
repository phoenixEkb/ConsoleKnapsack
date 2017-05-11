﻿using GAMultidimKnapsack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAMultidimKnapsack
{
    class SetPartition
    {
        static Random rand = new Random();
        static ConcurrentQueue<double> averageValuations = new ConcurrentQueue<double>();
        static ConcurrentQueue<double> maxValuations = new ConcurrentQueue<double>();
        static ConcurrentQueue<double> ages = new ConcurrentQueue<double>();

        static void Algorithm(int itemsAmount, int dimensions, double maxCost, double[] restrictions, double[] costs, double[,] itemsSet)
        {
            int ConfigsAmount = 10;
            double mutationPercent = 0.20;//FROM 0 TO 1
            GeneticalAlgorithm ga = new GeneticalAlgorithm(itemsAmount, dimensions, restrictions, costs, itemsSet, ConfigsAmount, GeneticalAlgorithm.TwoPointCrossover, GeneticalAlgorithm.SinglePointMutation, mutationPercent);
            int iterationNumber = 0;

            while (ga.GetAbsoluteMaximalKnapsackCost() != maxCost)
            {
                //var watch = new Stopwatch();
                //watch.Start();
                ga.MakeIteration();
                iterationNumber++;
                if (iterationNumber % 10000 == 0)
                {
                    Console.WriteLine(iterationNumber + ") delta with avg is " + (maxCost - ga.GetAbsoluteAverageKnapsackCost()) + "\n delta with max is " + (maxCost - ga.GetAbsoluteMaximalKnapsackCost()));
                    var bestCosts = ga.GetBestConfigsCosts();
                    Console.WriteLine("Top 3 of the best configs pool are {0}, {1}, {2}",
                        (maxCost - bestCosts[0]),
                        (maxCost - bestCosts[1]),
                        (maxCost - bestCosts[2]));
                }
                //  watch.Stop();
            }
            Console.WriteLine("Finished in {0}",iterationNumber);
            Console.ReadKey();
        }

        static void multipleAlgorithms(int itemsAmount, int dimensions, double maxCost, double[] restrictions, double[] costs, double[,] itemsSet)//запускаемм несколько алгоритмов с разными приближениями
        {
            int ConfigsAmount = 10, algorithmsNumber = 3;
            double mutationPercent = 0.20;

            GeneticalAlgorithm[] gas = new GeneticalAlgorithm[algorithmsNumber];
            for (int i = 0; i < algorithmsNumber; i++)//пока предположим, что первые приближения различны.
                gas[i] = new GeneticalAlgorithm(itemsAmount, dimensions, restrictions, costs, itemsSet, ConfigsAmount, GeneticalAlgorithm.TwoPointCrossover, GeneticalAlgorithm.SinglePointMutation, mutationPercent);
            int iterationNumber = 1;
            var controlWatch = new Stopwatch();
            controlWatch.Start();
            while (!gas.Select(x => x.GetAbsoluteMaximalKnapsackCost()).ToArray().Contains(maxCost))
            {
                Parallel.ForEach(gas, ga =>
              //foreach(GeneticalAlgorithm ga in gas)
                 {
                     ga.MakeIteration();
                     if (iterationNumber % 10 == 0)
                     {
                         Console.WriteLine(iterationNumber + ") delta with avg is " + (maxCost - ga.GetAbsoluteAverageKnapsackCost()) + "\n delta with max is " + (maxCost - ga.GetAbsoluteMaximalKnapsackCost()));
                         var bestCosts = ga.GetBestConfigsCosts();
                         Console.WriteLine("Top 3 of the best configs pool are {0}, {1}, {2}",
                             (maxCost - bestCosts[0]),
                             (maxCost - bestCosts[1]),
                             (maxCost - bestCosts[2]));
                     }
                 });
                iterationNumber++;
            }
            controlWatch.Stop();
            using (StreamWriter file1 = new StreamWriter(@"C:\Users\black_000\Documents\visual studio 2015\Projects\ConsoleKnapsack\ConsoleKnapsack\out.txt", true))
                file1.WriteLine(iterationNumber + " iterations, " + controlWatch.ElapsedMilliseconds + " ms");
        }

        static void algorithmWithRestart(int itemsAmount, int dimensions, double maxCost, double[] restrictions, double[] costs, double[,] itemsSet)
        {
            int ConfigsAmount = 10, restartTime=100000, currentMaxValueLiveLength=0;
            double PrevCost = 0; 
            double mutationPercent = 0.20;//FROM 0 TO 1
            GeneticalAlgorithm ga = new GeneticalAlgorithm(itemsAmount, dimensions, restrictions, costs, itemsSet, ConfigsAmount, GeneticalAlgorithm.TwoPointCrossover, GeneticalAlgorithm.SinglePointMutation, mutationPercent);
            int iterationNumber = 0, endIteration=10000000;
            List<double> resetPoints=new List<double>();
            while (ga.GetAbsoluteMaximalKnapsackCost() != maxCost&&iterationNumber<endIteration)
            {
                //var watch = new Stopwatch();
                //watch.Start();
                ga.MakeIteration();
                iterationNumber++;
                double tmp = ga.GetBestConfigsCosts()[0];
                if (tmp != PrevCost)
                {
                    PrevCost = tmp;
                    currentMaxValueLiveLength = 0;
                }
                else
                {
                    currentMaxValueLiveLength++;
                }

                //if (iterationNumber % 10000 == 0)//Отрисовка
                //{
                //    Console.Write(iterationNumber + ") ");// delta with avg is " + (maxCost - ga.GetAbsoluteAverageKnapsackCost()) + "\n delta with max is " + (maxCost - ga.GetAbsoluteMaximalKnapsackCost()));
                //    var bestCosts = ga.GetBestConfigsCosts();
                //    Console.WriteLine("Top 3 of the best configs pool are {0}, {1}, {2}, {3}, {4}",
                //        (maxCost - bestCosts[0]),
                //        (maxCost - bestCosts[1]),
                //        (maxCost - bestCosts[2]),
                //        (maxCost - bestCosts[3]),
                //        (maxCost - bestCosts[4]));
                //}

                if (currentMaxValueLiveLength==restartTime)
                {
                    var restartPercent = 0.4;
                    resetPoints.Add(maxCost - ga.GetBestConfigsCosts()[0]);
                    ga.RestartAlgorithm(restartPercent);
                    PrevCost = 0;
                    currentMaxValueLiveLength = 0;
                    //Console.WriteLine("Restart");

                }
                //  watch.Stop();
            }
            Console.WriteLine("Finished in {0}", iterationNumber);
            foreach (var x in resetPoints)
                Console.Write("{0}, ",x);
            Console.WriteLine(resetPoints.Count);
            //Console.ReadKey();
        }

        static void TestAlgorithm()//Proof of concept
        {
            int itemsAmount = 500, dimensions = 6;
            double[] restrictions = new double[] { 100, 600, 1200, 2400, 500, 2000 }, costs = new double[itemsAmount];
            for (int i = 0; i < itemsAmount; i++)
                costs[i] = rand.NextDouble() * 30;
            double[,] itemsSet = new double[itemsAmount, dimensions];
            for (int i = 0; i < itemsAmount; i++)
                for (int j = 0; j < dimensions; j++)
                    itemsSet[i, j] = rand.NextDouble() * 50;
            int ConfigsAmount = 6;
            GeneticalAlgorithm ga = new GeneticalAlgorithm(itemsAmount, dimensions, restrictions, costs, itemsSet, ConfigsAmount, GeneticalAlgorithm.FixedSinglePointCrossover, GeneticalAlgorithm.SinglePointMutation, 0.75);

            int iterationNumber = 0;
            while (true)
            {
                var watch = new Stopwatch();
                watch.Start();

                while (watch.ElapsedMilliseconds < 200)
                {
                    ga.MakeIteration();
                    iterationNumber++;
                    averageValuations.Enqueue(ga.GetNormaizedAveragePoolCost());
                    maxValuations.Enqueue(ga.GetNormalizedMaximalKnapsackCost());
                }
                watch.Stop();
            }
        }

        static void ProcessTestSet(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                int experimentsAmount = Convert.ToInt32(sr.ReadLine());

                for (int experiment = 0; experiment < experimentsAmount; experiment++)
                {
                    string[] initializationSequence;
                    string firstString = sr.ReadLine();
                    if (firstString.Trim() == "")
                        initializationSequence = sr.ReadLine().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    else initializationSequence = firstString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); ;
                    int itemsAmount = Convert.ToInt32(initializationSequence[0]),
                    dimensions = Convert.ToInt32(initializationSequence[1]);
                    double maxCost = Convert.ToDouble(initializationSequence[2]);

                    List<double> tempCosts = new List<double>();
                    while (tempCosts.Count() != itemsAmount)
                        tempCosts.AddRange(sr
                            .ReadLine()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Convert.ToDouble(x))
                            .ToList());
                    double[] costs = tempCosts.ToArray();

                    double[,] itemsSet = new double[itemsAmount, dimensions];
                    for (int i = 0; i < dimensions; i++)
                    {
                        int itemsReaden = 0;
                        while (itemsReaden != itemsAmount)
                        {
                            double[] currentString = sr.ReadLine()
                                .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).
                                Select(x => Convert.ToDouble(x)).
                                ToArray();
                            for (int j = itemsReaden, k = 0; j < currentString.Count() + itemsReaden; j++, k++)
                                itemsSet[j, i] = currentString[k];
                            itemsReaden += currentString.Count();
                        }
                    }
                    List<double> tempRestrictions = new List<double>();
                    while (tempRestrictions.Count() != dimensions)
                        tempRestrictions.AddRange(sr
                            .ReadLine()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Convert.ToDouble(x))
                            .ToList());
                    double[] restrictions = tempRestrictions.ToArray();
                    Algorithm(itemsAmount, dimensions, maxCost, restrictions, costs, itemsSet);
                    Thread.Sleep(3000);
                    maxValuations.Enqueue(0);
                    averageValuations.Enqueue(0);
                }
            }
        }

        static void ProcessTestSet(string inputFileData, string inputFileResults)//WORK WITH IT!
        {
            using (StreamReader dataReader = new StreamReader(inputFileData))
            {
                string[] resultsArray = File.ReadAllLines(inputFileResults);
                var resultsStringNumber = 12;
                int experimentsAmount = Convert.ToInt32(dataReader.ReadLine());

                for (int experiment = 0; experiment < experimentsAmount; experiment++, resultsStringNumber++)
                {
                    string[] initializationSequence;
                    string firstString = dataReader.ReadLine();
                    if (firstString.Trim() == "")
                        initializationSequence = dataReader.ReadLine().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    else initializationSequence = firstString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); ;
                    int itemsAmount = Convert.ToInt32(initializationSequence[0]),
                    dimensions = Convert.ToInt32(initializationSequence[1]);


                    double maxCost = Convert.ToDouble(resultsArray[resultsStringNumber].Substring(25));//Convert.ToDouble(temp);

                    List<double> tempCosts = new List<double>();
                    while (tempCosts.Count() != itemsAmount)
                        tempCosts.AddRange(dataReader
                            .ReadLine()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Convert.ToDouble(x))
                            .ToList());
                    double[] costs = tempCosts.ToArray();

                    double[,] itemsSet = new double[itemsAmount, dimensions];
                    for (int i = 0; i < dimensions; i++)
                    {
                        int itemsReaden = 0;
                        while (itemsReaden != itemsAmount)
                        {
                            double[] currentString = dataReader.ReadLine()
                                .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).
                                Select(x => Convert.ToDouble(x)).
                                ToArray();
                            for (int j = itemsReaden, k = 0; j < currentString.Count() + itemsReaden; j++, k++)
                                itemsSet[j, i] = currentString[k];
                            itemsReaden += currentString.Count();
                        }
                    }
                    List<double> tempRestrictions = new List<double>();
                    while (tempRestrictions.Count() != dimensions)
                        tempRestrictions.AddRange(dataReader
                            .ReadLine()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Convert.ToDouble(x))
                            .ToList());
                    double[] restrictions = tempRestrictions.ToArray();
                    algorithmWithRestart(itemsAmount, dimensions, maxCost, restrictions, costs, itemsSet);
                    Thread.Sleep(3000);
                    maxValuations.Enqueue(0);
                    averageValuations.Enqueue(0);
                }
            }
        }


        static void Main()
        {
            //new Thread(TestAlgorithm) { IsBackground = true }.Start(); 
            //new Thread(() => ProcessTestSet(@"C:\Users\black_000\Source\Repos\GeneticKnapsack\GAMultidimKnapsack\3.txt", @"C:\Users\black_000\Source\Repos\GeneticKnapsack\GAMultidimKnapsack\_res.txt")) { IsBackground = true }.Start();
            ProcessTestSet(@"C:\Users\black_000\Source\Repos\GeneticKnapsack\GAMultidimKnapsack\3.txt", @"C:\Users\black_000\Source\Repos\GeneticKnapsack\GAMultidimKnapsack\_res.txt");
        }
    }
}
