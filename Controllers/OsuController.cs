using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SakuraAPI.Osu;

namespace SakuraAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OsuController : ControllerBase
    {
        private int[] mode = { 0, 1, 2, 3 };

        [HttpGet]
        public string PerformanceCalculator(int BeatmapID, int Mode, double Accuracy, int Combo, int C300, int C100, int C50, int Miss,
            int Geki, int Katu, string? Mods = "", int? Score = 1000000, bool isPlay = false)
        {
            string result;
            if (BeatmapID == 0)
            {
                result = new Dictionary<string, string>
                {
                    { "error", "Please Enter BeatmapID!" }
                }.ToString()!;
            }
            else if (mode.Contains(Mode))
                result = PlayerDataPerformanceCalculator.Calculator(BeatmapID, Mode, isPlay, Accuracy, Combo, C300, C100, C50, Miss, Geki, Katu, Mods, Score).ToString();
            else
            {
                result = new Dictionary<string, string>
                {
                    { "error", "Mode error! osu: 0, taiko: 1, catch: 2, mania: 3" }
                }.ToString()!;
            }
            return result;
        }
    }
}
