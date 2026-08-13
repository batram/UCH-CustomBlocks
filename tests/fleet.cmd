@echo off
rem Runs the Harmonic Sheep Fleet harness from the sibling checkout, with this
rem repo as the project (tests\suites, tests\baselines, tests\artifacts).
dotnet run --project "%~dp0..\..\UCH-HarmonicSheepFleet\src\Fleet" -- %*
