@echo off

set data="data\\berlin52.txt"
REM Available parameters = -tournament, -roulette
set selection=tournament
REM Available parameters = -PMX, -OX
set crossover=PMX

@echo on
start "TSP_Berlin52" Berlin.exe %data% -%selection% -%crossover%