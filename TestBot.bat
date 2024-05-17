@echo off
cd "C:\Program Files (x86)\Cute Chess"
cutechess-cli -engine conf="GhostEngine NEW TEST" -engine conf="GhostEngine OLD BASE" -each proto=uci tc=8+0.08 -openings file="C:\Users\thoma\OneDrive\Desktop\Cutechess testing\OPENINGS.epd" format=epd order=random -games 200 -rounds 1 -maxmoves 200 -sprt elo0=0 elo1=10 alpha=0.05 beta=0.05 -concurrency 4 -ratinginterval 10
pause
