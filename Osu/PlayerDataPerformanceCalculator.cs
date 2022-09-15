using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Scoring;
using osu.Game.Rulesets.Scoring;

namespace SakuraAPI.Osu
{
    public static class PlayerDataPerformanceCalculator
    {
        public static ProcessorWorkingBeatmap? workingBeatmap;
        public static JObject Calculator(int BeatmapID, int Mode, bool isPlay, double Accuracy, int Combo, int C300, int C100, int C50, int Miss,
            int Geki, int Katu, string? Mods, int? ManiaScore)
        {
            workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(BeatmapID.ToString());
            PerformanceCalculator PPCalculator;
            DifficultyAttributes DiffAttributes;
            PerformanceAttributes PPAttributes;
            Score score = new();

            // 记录游玩的模式
            score.ScoreInfo.Ruleset = workingBeatmap.BeatmapInfo.OnlineID != 0 ?
                LegacyHelper.GetRulesetFromLegacyID(Mode).RulesetInfo : workingBeatmap.BeatmapInfo.Ruleset;
            // 记录游玩的最高 Combo
            score.ScoreInfo.MaxCombo = Combo;
            // 记录 Mania 游玩分数
            score.ScoreInfo.TotalScore = ManiaScore ?? 0;
            
            // 记录 Mods
            List<string> sMods = new();
            if (Mods != null)
            {
                Mods = Mods.ToUpper();
                string temp = "";
                for (int i = 0; i < Mods.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        temp += Mods[i];
                    }
                    else
                    {
                        sMods.Add(temp += Mods[i]);
                        temp = "";
                    }
                }
            }
            sMods.Add("CL");
            JArray ModsList = new();
            foreach (string Mod in sMods)
            {
                ModsList.Add(new JObject() {
                    { "acronym", Mod },
                    { "settings", new JObject() }
                });
            }
            score.ScoreInfo.ModsJson = ModsList.ToString();

            // 记录各模式游玩记录
            Dictionary<HitResult, int> Statistics = new();
            if (isPlay)
            {
                switch (Mode)
                {
                    case 0:
                        Statistics = new()
                        {
                            { HitResult.Great, C300 },
                            { HitResult.Ok, C100 },
                            { HitResult.Meh, C50 },
                            { HitResult.Miss, Miss }
                        };
                        Accuracy = GetOsuAccuracy(Statistics);
                        break;
                    case 1:
                        Statistics = new()
                        {
                            { HitResult.Great, C300 },
                            { HitResult.Ok, C100 },
                            { HitResult.Meh, C50 },
                            { HitResult.Miss, Miss }
                        };
                        break;
                    case 2:
                        Statistics = new()
                        {
                            { HitResult.Great, C300 },
                            { HitResult.LargeTickHit, C100 },
                            { HitResult.SmallTickHit, C50 },
                            { HitResult.SmallTickMiss, Katu },
                            { HitResult.Miss, Miss }
                        };
                        break;
                    case 3:
                        Statistics = new()
                        {
                            { HitResult.Perfect, C300 },
                            { HitResult.Great, Geki },
                            { HitResult.Ok, Katu },
                            { HitResult.Good, C100 },
                            { HitResult.Meh, C50 },
                            { HitResult.Miss, Miss }
                        };
                        break;
                }
                score.ScoreInfo.Accuracy = Accuracy;
            }
            else
            {
                switch (Mode)
                {
                    case 0:
                        Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Ok, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                        break;
                    case 1:
                        Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Ok, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                        break;
                    case 2:
                        Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.LargeTickHit, 0 },
                        { HitResult.SmallTickHit, 0 },
                        { HitResult.SmallTickMiss, 0 },
                        { HitResult.Miss, 0 }
                    };
                        break;
                    case 3:
                        Statistics = new()
                    {
                        { HitResult.Perfect, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Great, 0 },
                        { HitResult.Ok, 0 },
                        { HitResult.Good, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                        break;
                }
                score.ScoreInfo.Accuracy = 1;
            }
            score.ScoreInfo.Statistics = Statistics;

            // 计算已记录数据的地图难度结果
            DiffAttributes = score.ScoreInfo.Ruleset.CreateInstance().CreateDifficultyCalculator(workingBeatmap).Calculate(score.ScoreInfo.Mods.ToArray());

            double HP = workingBeatmap.BeatmapInfo.Difficulty.DrainRate;
            double CS = workingBeatmap.BeatmapInfo.Difficulty.CircleSize;
            // 计算开启 HR 或 EZ mod后的 CS 和 HP 难度
            if (sMods.Contains("HR"))
            {
                HP = (HP * 0.4) + HP;

                if (score.ScoreInfo.RulesetID == 0 || score.ScoreInfo.RulesetID == 2)
                {
                    CS = (CS * 0.3) + CS;
                }
            }
            else if (sMods.Contains("EZ"))
            {
                HP -= (HP * 0.5);

                if (score.ScoreInfo.RulesetID == 0 || score.ScoreInfo.RulesetID == 2)
                {
                    CS -= (CS * 0.5);
                }
            }

            JObject result = new()
            {
                { "StarRating", double.Parse(DiffAttributes.StarRating.ToString()) },
                { "HP", HP },
                { "CS", CS }
            };

            int MaxCombo = 0;
            switch (DiffAttributes)
            {
                case OsuDifficultyAttributes osu:
                    result.Add("Aim", osu.AimDifficulty);
                    result.Add("Speed", osu.SpeedDifficulty);
                    result.Add("AR", osu.ApproachRate);
                    result.Add("OD", osu.OverallDifficulty);
                    MaxCombo = osu.MaxCombo + osu.SliderCount;
                    break;
                case TaikoDifficultyAttributes taiko:
                    result.Add("MaxCombo", taiko.MaxCombo);
                    result.Add("HitWindow", taiko.GreatHitWindow);
                    result.Add("AR", taiko.ApproachRate);
                    MaxCombo = taiko.MaxCombo;
                    break;
                case CatchDifficultyAttributes @catch:
                    result.Add("MaxCombo", @catch.MaxCombo);
                    result.Add("AR", @catch.ApproachRate);
                    MaxCombo = @catch.MaxCombo;
                    break;
            }
            if (isPlay)
            {
                score.ScoreInfo.MaxCombo = Combo;
            }
            else
            {
                score.ScoreInfo.MaxCombo = MaxCombo;
            }

            PPCalculator = score.ScoreInfo.Ruleset.CreateInstance().CreatePerformanceCalculator();
            // 计算已记录数据的PP结果
            PPAttributes = PPCalculator.Calculate(score.ScoreInfo, workingBeatmap);
            Dictionary<string, object> PPAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(PPAttributes)) ?? new Dictionary<string, object>();
            foreach (var attr in PPAttributeValues)
            {
                result.Add(attr.Key, double.Parse(attr.Value.ToString()!));
            }

            result.Add("sspp", SSPP(Mode, MaxCombo, ModsList, score));
            result.Add("ifpp", IFPP(Mode, Statistics, MaxCombo, ModsList, score));

            return result;
        }

        public static double SSPP(int Mode, int MaxCombo, JArray ModsList, Score score)
        {
            PerformanceCalculator PPCalculator;
            PerformanceAttributes PPAttributes;
            Score score2 = new();

            Dictionary<HitResult, int> Statistics = new();
            switch (Mode)
            {
                case 0:
                    Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Ok, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                    break;
                case 1:
                    Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Ok, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                    break;
                case 2:
                    Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.LargeTickHit, 0 },
                        { HitResult.SmallTickHit, 0 },
                        { HitResult.SmallTickMiss, 0 },
                        { HitResult.Miss, 0 }
                    };
                    break;
                case 3:
                    Statistics = new()
                    {
                        { HitResult.Perfect, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Great, 0 },
                        { HitResult.Ok, 0 },
                        { HitResult.Good, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                    break;
            }
            score2.ScoreInfo.Ruleset = score.ScoreInfo.Ruleset;
            score2.ScoreInfo.Accuracy = 1;
            score2.ScoreInfo.MaxCombo = MaxCombo;
            score2.ScoreInfo.Statistics = Statistics;
            score2.ScoreInfo.ModsJson = ModsList.ToString();

            PPCalculator = score2.ScoreInfo.Ruleset.CreateInstance().CreatePerformanceCalculator();
            // 计算已记录数据的PP结果
            PPAttributes = PPCalculator?.Calculate(score2.ScoreInfo, workingBeatmap)!;

            return double.Parse(PPAttributes.Total.ToString());
        }

        public static double IFPP(int Mode, Dictionary<HitResult, int> Statistics, int MaxCombo, JArray ModsList, Score score)
        {
            PerformanceCalculator PPCalculator;
            PerformanceAttributes PPAttributes;
            Score score2 = new();

            int Ok, Meh = 0;
            double Accuracy = 1;
            switch (Mode)
            {
                case 0:
                    Ok = Statistics.GetValueOrDefault(HitResult.Ok);
                    Meh = Statistics.GetValueOrDefault(HitResult.Meh);
                    Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count - Ok - Meh },
                        { HitResult.Ok, Ok },
                        { HitResult.Meh, Meh },
                        { HitResult.Miss, 0 }
                    };
                    Accuracy = GetOsuAccuracy(Statistics);
                    break;
                case 1:
                    Ok = Statistics.GetValueOrDefault(HitResult.Ok);
                    Meh = Statistics.GetValueOrDefault(HitResult.Meh);
                    Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count - Ok - Meh },
                        { HitResult.Ok, Ok },
                        { HitResult.Meh, Meh },
                        { HitResult.Miss, 0 }
                    };
                    Accuracy = GetTaikoAccuracy(Statistics);
                    break;
                case 2:
                    int LargeTickHit = Statistics.GetValueOrDefault(HitResult.LargeTickHit);
                    int SmallTickHit = Statistics.GetValueOrDefault(HitResult.SmallTickHit);
                    Statistics = new()
                    {
                        { HitResult.Great, workingBeatmap.Beatmap.HitObjects.Count - LargeTickHit - SmallTickHit },
                        { HitResult.LargeTickHit, LargeTickHit },
                        { HitResult.SmallTickHit, SmallTickHit },
                        { HitResult.SmallTickMiss, 0 },
                        { HitResult.Miss, 0 }
                    };
                    Accuracy = GetCatchAccuracy(Statistics);
                    break;
                case 3:
                    Statistics = new()
                    {
                        { HitResult.Perfect, workingBeatmap.Beatmap.HitObjects.Count },
                        { HitResult.Great, 0 },
                        { HitResult.Ok, 0 },
                        { HitResult.Good, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    };
                    break;
            }
            score2.ScoreInfo.Ruleset = score.ScoreInfo.Ruleset;
            score2.ScoreInfo.Accuracy = Accuracy;
            score2.ScoreInfo.MaxCombo = MaxCombo;
            score2.ScoreInfo.Statistics = Statistics;
            score2.ScoreInfo.ModsJson = ModsList.ToString();

            PPCalculator = score2.ScoreInfo.Ruleset.CreateInstance().CreatePerformanceCalculator();
            // 计算已记录数据的PP结果
            PPAttributes = PPCalculator?.Calculate(score2.ScoreInfo, workingBeatmap)!;

            return double.Parse(PPAttributes.Total.ToString());
        }

        private static double GetOsuAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMeh = statistics[HitResult.Meh];
            var countMiss = statistics[HitResult.Miss];
            var total = countGreat + countGood + countMeh + countMiss;

            return (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * total);
        }

        private static double GetTaikoAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMiss = statistics[HitResult.Miss];
            var total = countGreat + countGood + countMiss;

            return (double)((2 * countGreat) + countGood) / (2 * total);
        }

        private static double GetCatchAccuracy(Dictionary<HitResult, int> statistics)
        {
            double hits = statistics[HitResult.Great] + statistics[HitResult.LargeTickHit] + statistics[HitResult.SmallTickHit];
            double total = hits + statistics[HitResult.Miss] + statistics[HitResult.SmallTickMiss];

            return hits / total;
        }

    }
}
