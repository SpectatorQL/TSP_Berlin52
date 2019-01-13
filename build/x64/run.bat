@echo off

set data="data\\berlin52.txt"
set populationSize=40
set mutationChance=0.3
set selection=tournament
set crossover=PMX

call Berlin.exe %data% %populationSize% %mutationChance% -%selection% -%crossover%
