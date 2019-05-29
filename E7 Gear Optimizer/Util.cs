using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net;

namespace E7_Gear_Optimizer
{
    public enum ItemType
    {
        Weapon,
        Helmet,
        Armor,
        Necklace,
        Ring,
        Boots,
        All,
        Artifact
    }

    public enum Set
    {
        Speed,
        Hit,
        Crit,
        Def,
        Health,
        Attack,
        Counter,
        Lifesteal,
        Destruction,
        Resist,
        Rage,
        Immunity,
        Unity
    }

    public enum Grade
    {
        Epic,
        Heroic,
        Rare,
        Good,
        Normal
    }

    public enum Stats
    {
        ATKPercent,
        ATK,
        SPD,
        Crit,
        CritDmg,
        HPPercent,
        HP,
        DEFPercent,
        DEF,
        EFF,
        RES,
        EHP,
        DMG
    }

    public enum Element
    {
        Fire,
        Earth,
        Ice,
        Light,
        Dark,
        All
    }

    public enum HeroClass
    {
        Knight,
        Warrior,
        Thief,
        Mage,
        SoulWeaver,
        Ranger,
        All
    }
    public static class Util
    {
        public static string ApiUrl = System.Configuration.ConfigurationManager.AppSettings["ApiUrl"];
        public static string AssetUrl = System.Configuration.ConfigurationManager.AppSettings["AssetUrl"];
        public static Bitmap error = Properties.Resources.error;
        public static Bitmap star = Properties.Resources.star;
        public static Bitmap star_j = Properties.Resources.star_j;
        public static WebClient client = new WebClient();

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Dictionary<Grade, Color> gradeColors = new Dictionary<Grade, Color>()
        {
            [Grade.Epic] = Color.Red,
            [Grade.Heroic] = Color.Purple,
            [Grade.Rare] = Color.Blue,
            [Grade.Good] = Color.Green,
            [Grade.Normal] = Color.Gray
        };

        public static Dictionary<Stats, string> statStrings = new Dictionary<Stats, string>()
        {
            [Stats.ATK] = "Attack",
            [Stats.ATKPercent] = "Attack",
            [Stats.Crit] = "Critical Hit Chance",
            [Stats.CritDmg] = "Critical Hit Damage",
            [Stats.DEF] = "Defense",
            [Stats.DEFPercent] = "Defense",
            [Stats.EFF] = "Effectiveness",
            [Stats.HP] = "Health",
            [Stats.HPPercent] = "Health",
            [Stats.RES] = "Effect Resistance",
            [Stats.SPD] = "Speed"
        };

        public static List<Stats> percentStats = new List<Stats>()
        {
            Stats.ATKPercent,
            Stats.Crit,
            Stats.CritDmg,
            Stats.DEFPercent,
            Stats.HPPercent,
            Stats.EFF,
            Stats.RES
        };

        public static List<Set> fourPieceSets = new List<Set>() { Set.Attack, Set.Destruction, Set.Lifesteal, Set.Rage, Set.Speed };

        //Calculate the active Sets in a given gear combination
        public static List<Set> activeSet(List<Item> gear)
        {
            List<Set> activeSets = new List<Set>();
            Dictionary<Set, int> setCounter = new Dictionary<Set, int>();
            foreach (Set s in Enum.GetValues(typeof(Set)))
            {
                setCounter[s] = 0;
            }
            foreach (Item item in gear)
            {
                setCounter[item.Set] += 1;
            }
            foreach (Set set in Enum.GetValues(typeof(Set)))
            {
                if (Util.fourPieceSets.Contains(set) && setCounter[set] / 4 > 0)
                {
                    activeSets.Add(set);
                }
                else if (!Util.fourPieceSets.Contains(set))
                {
                    for (int i = 0; i < setCounter[set] / 2; i++)
                    {
                        activeSets.Add(set);
                    }
                }
            }
            return activeSets;
        }
    }
}