using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
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
        HPpS,
        EHP,
        EHPpS,
        DMG,
        DMGpS
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
        public static string GitHubUrl = System.Configuration.ConfigurationManager.AppSettings["GitHubUrl"];
        public static string ver = System.Configuration.ConfigurationManager.AppSettings["Version"];
        public static Bitmap error = Properties.Resources.error;
        public static Bitmap star = Properties.Resources.star;
        public static Bitmap star_j = Properties.Resources.star_j;
        public static WebClient client = new WebClient();
        public static List<string> percentageColumns = new List<string>() {"c_Ilvl", "c_Enhance", "c_Value", "c_ATKPer", "c_ATK", "c_SPD", "c_CHC", "c_CHD", "c_HPPer", "c_HP", "c_DEFPer", "c_DEF", "c_EFF", "c_RES", "c_WSS" };
        public static Dictionary<ItemType, List<Stats>> rollableStats = new Dictionary<ItemType, List<Stats>>()
        {
            [ItemType.Weapon] = new List<Stats>() { Stats.Crit, Stats.CritDmg, Stats.ATK, Stats.ATKPercent, Stats.EFF, Stats.HP, Stats.HPPercent, Stats.RES, Stats.SPD },
            [ItemType.Helmet] = new List<Stats>() { Stats.Crit, Stats.CritDmg, Stats.DEF, Stats.DEFPercent, Stats.EFF, Stats.HP, Stats.HPPercent, Stats.RES, Stats.SPD, Stats.ATK, Stats.ATKPercent },
            [ItemType.Armor] = new List<Stats>() { Stats.Crit, Stats.CritDmg, Stats.DEF, Stats.DEFPercent, Stats.EFF, Stats.HP, Stats.HPPercent, Stats.RES, Stats.SPD },
            [ItemType.Necklace] = new List<Stats>() { Stats.Crit, Stats.CritDmg, Stats.DEF, Stats.DEFPercent, Stats.EFF, Stats.HP, Stats.HPPercent, Stats.RES, Stats.SPD, Stats.ATK, Stats.ATKPercent },
            [ItemType.Ring] = new List<Stats>() { Stats.Crit, Stats.CritDmg, Stats.DEF, Stats.DEFPercent, Stats.EFF, Stats.HP, Stats.HPPercent, Stats.RES, Stats.SPD, Stats.ATK, Stats.ATKPercent },
            [ItemType.Boots] = new List<Stats>() { Stats.Crit, Stats.CritDmg, Stats.DEF, Stats.DEFPercent, Stats.EFF, Stats.HP, Stats.HPPercent, Stats.RES, Stats.SPD, Stats.ATK, Stats.ATKPercent }
        };
        //Cached value of Set enum length to use in arrays' initializations instead of magic number
        public static readonly int SETS_LENGTH = Enum.GetValues(typeof(Set)).Length;

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

        public static HashSet<Set> fourPieceSets = new HashSet<Set>() { Set.Attack, Set.Destruction, Set.Lifesteal, Set.Rage, Set.Speed, Set.Counter };

        //Faster alternative to fourPieceSets.Contains() or Dictionary<Set, bool> to determine if a set is 4-piece set. Each index represents (int)Set
        private static readonly bool[] isFourPieceSetArray;

        static Util()
        {
            isFourPieceSetArray = new bool[SETS_LENGTH];
            for (int i = 0; i < isFourPieceSetArray.Length; i++)
            {
                isFourPieceSetArray[i] = fourPieceSets.Contains((Set)i);
            }
        }

        //Calculate the active Sets in a given gear combination
        public static List<Set> activeSet(IEnumerable<Item> gear)
        {
            Dictionary<Set, int> setCounter = new Dictionary<Set, int>(6);
            foreach (Item item in gear)
            {
                updateSetCounter(setCounter, item);
            }
            return activeSet(setCounter);
        }

        public static void updateSetCounter(Dictionary<Set, int> setCounter, Item item)
        {
            if (setCounter.ContainsKey(item.Set))
            {
                setCounter[item.Set]++;
            }
            else
            {
                setCounter.Add(item.Set, 1);
            }
        }

        public static List<Set> activeSet(Dictionary<Set, int> setCounter)
        {
            List<Set> activeSets = new List<Set>(3);
            foreach (var setCount in setCounter)
            {
                bool isFourPieceSet = isFourPieceSetArray[(int)setCount.Key];
                if (isFourPieceSet && setCount.Value / 4 > 0)
                {
                    activeSets.Add(setCount.Key);
                }
                else if (!isFourPieceSet)
                {
                    for (int i = 0; i < setCount.Value / 2; i++)
                    {
                        activeSets.Add(setCount.Key);
                    }
                }
            }
            return activeSets;
        }

        public static List<Set> activeSet(int[] setCounter)
        {
            List<Set> activeSets = new List<Set>();
            for (int iSet = 0; iSet < setCounter.Length; iSet++)
            {
                if (setCounter[iSet] == 0)
                {
                    continue;
                }
                bool isFourPieceSet = isFourPieceSetArray[iSet];
                if (isFourPieceSet && setCounter[iSet] / 4 > 0)
                {
                    activeSets.Add((Set)iSet);
                }
                else if (!isFourPieceSet)
                {
                    for (int i = 0; i < setCounter[iSet] / 2; i++)
                    {
                        activeSets.Add((Set)iSet);
                    }
                }
            }
            return activeSets;
        }

        public static int setSlots(List<Set> activeSets)
        {
            int setSlots = 0;
            foreach(Set s in activeSets)
            {
                if (Util.fourPieceSets.Contains(s))
                {
                    setSlots += 4;
                }
                else
                {
                    setSlots += 2;
                }
            }
            return setSlots;
        }

        public static string toAPIUrl(string str)
        {
            return str.ToLower().Replace('&', ' ').Replace("   ", " ").Replace(' ', '-');
        }
    }
}
