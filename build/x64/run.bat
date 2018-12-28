@echo off

set data="data\\berlin52.txt"
set populationSize=40
set mutationChance=0.04
set selection=tournament
set crossover=PMX

@echo on
start "TSP_Berlin52" Berlin.exe %data% %populationSize% %mutationChance% -%selection% -%crossover%