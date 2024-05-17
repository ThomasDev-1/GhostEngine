using System.Numerics;
using System;
using System.IO;

namespace ChessChallenge.Application
{
    public static class Version
    {
        public static int version = 1;

    }
    public static class MenuUI
    {
        public static void DrawButtons(ChallengeController controller)
        {
            Vector2 buttonPos = UIHelper.Scale(new Vector2(180, 40));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(260, 55));
            Vector2 newButtonSize = UIHelper.Scale(new Vector2(300, 55));
            Vector2 newButtonPos = UIHelper.Scale(new Vector2(180, 200));
            
            Vector2 fancyButtonSize = UIHelper.Scale(new Vector2(200, 55));

            Vector2 sideButtonSize = UIHelper.Scale(new Vector2(39, 55));
            Vector2 sideButtonSpace1 = UIHelper.Scale(new Vector2(50, 398));
            Vector2 sideButtonSpace2 = UIHelper.Scale(new Vector2(310, 398));
            float spacing = buttonSize.Y * 1.2f;

            // Game Buttons
            if (NextButtonInRow("Human vs MyBot", ref buttonPos, spacing, buttonSize))
            {
                var whiteType = controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
                var blackType = !controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
                controller.StartNewGame(whiteType, blackType);
            }
            if (NextButtonInRow("MyBot vs MyBot", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.MyBot);
            }

            // Page buttons
            buttonPos.Y += 500;

            if (NextButtonInRow("Save Games", ref buttonPos, spacing, buttonSize))
            {
                string pgns = controller.AllPGNs;
                string directoryPath = Path.Combine(FileHelper.AppDataPath, "Games");
                Directory.CreateDirectory(directoryPath);
                string fileName = FileHelper.GetUniqueFileName(directoryPath, "games", ".txt");
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, pgns);
                ConsoleHelper.Log("Saved games to " + fullPath, false, ConsoleColor.Blue);
            }

            if (NextButtonInRow("Exit (ESC)", ref buttonPos, spacing, buttonSize))
            {
                Environment.Exit(0);
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y += spacingY;
                return pressed;
            }

            buttonPos = UIHelper.Scale(new Vector2(405, 210));
            buttonSize = UIHelper.Scale(new Vector2(200, 55));
            if (NextButtonInRow("MyBot vs StockFish", ref newButtonPos, spacing, newButtonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.StockFish);
            }
            if (NextButtonInRow("MyBot vs LiteBlue", ref newButtonPos, spacing, newButtonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.LiteBlue);
            }
            if (NextButtonInRow("MyBot vs TestBot", ref newButtonPos, spacing, newButtonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.TestBot);
            }

            if (NextButtonInRow("<", ref sideButtonSpace1, 0, sideButtonSize))
            {
                if (Version.version > 1) Version.version--;
            }
            if (NextButtonInRow(">", ref sideButtonSpace2, 0, sideButtonSize))
            {
                if (Version.version < 6) Version.version++;
            }

            if (NextButtonInRow("MyBot Vs BB_V" + Version.version, ref newButtonPos, spacing, fancyButtonSize))
            {
                switch (Version.version)
                {
                    case 1:
                        controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.BettyBot_V1);
                        break;
                    case 2:
                        controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.BettyBot_V2);
                        break;
                    case 3:
                        controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.BettyBot_V3);
                        break;
                    case 4:
                        controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.BettyBot_V4);
                        break;
                    case 5:
                        controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.BettyBot_V5);
                        break;
                    case 6:
                        controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.BettyBot_V6);
                        break;

                }
            }

            if (NextButtonInRow("MyBot vs GE_V1", ref newButtonPos, spacing, newButtonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.GhostEngine_V1);
            }
        }
    }
}