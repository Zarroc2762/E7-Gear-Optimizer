using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace E7_Gear_Optimizer
{
    public class Hero
    {
        public string ID { get; }
        public string Name { get; }
        public Image Portrait { get; }
        public Image PortraitSmall { get; }
        public Element Element { get; }
        public HeroClass Class { get; }
        private Dictionary<ItemType, Item> gear;
        public Item Artifact { get; set; }
        private int lvl;
        private int awakening;
        private Image stars;
        public Image Stars { get => stars; }
        public Dictionary<Stats, decimal> BaseStats { get; }
        private Dictionary<Stats, decimal> currentStats;
        private Dictionary<Stats, decimal> AwakeningStats { get; set; }

        public Hero(string ID, string name, List<Item> gear, Item artifact, int lvl, int awakening)
        {
            this.ID = ID;
            Name = name;
            this.gear = new Dictionary<ItemType, Item>();
            foreach (Item item in gear)
            {
                this.gear[item.Type] = item;
                item.Equipped = this;
            }
            Artifact = artifact;
            this.Lvl = lvl;
            this.awakening = awakening > lvl / 10 ? lvl / 10 : awakening;
            try
            {
                string json = Util.client.DownloadString(Util.ApiUrl + "/hero/" + Util.toAPIUrl(Name));
                BaseStats = getBaseStats(json);
                Element = getElement(json);
                Class = getClass(json);
                AwakeningStats = getAwakeningStats(json);
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }
            Portrait = getPortrait(name);
            PortraitSmall = Util.ResizeImage(Portrait, 60, 60);
            stars = getStars(lvl, awakening);
            currentStats = calcStats();
        }
        public int Lvl
        {
            get => lvl;
            set => lvl = value > 49 && value < 61 ? value  : lvl;
        }
        public int Awakening
        {
            get => awakening;
            set => awakening = value > -1 && value < 7 ? value : awakening;
        }
        public Dictionary<Stats, decimal> CurrentStats { get => currentStats; }

        //Fetch the portrait of the hero from EpicSevenDB
        private Image getPortrait(string name)
        {
            Bitmap portrait;
            try
            {
                portrait = new Bitmap(Util.client.OpenRead(Util.AssetUrl + "/hero/" + Util.toAPIUrl(name) + "/icon.png"));
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
                portrait = Util.error;
            }
            return portrait;
        }

        //Create a Bitmap which contains an image of the overlapping Awakening stars
        private Image getStars(int lvl, int awakening)
        {
            Bitmap stars = new Bitmap(lvl / 10 * 16 + 2, 25);
            Graphics g = Graphics.FromImage(stars);
            for (int i = 0; i < lvl / 10; i++)
            {
                g.DrawImage(Util.star, new Point(i * 15, 0));
                if (i < awakening)
                {
                    g.DrawImage(Util.star_j, new Point(i * 15, 0));
                }
            }
            return stars;
        }

        //Parse JSON data from EpicSevenDB to get the base stats of the hero at lvl 50 or 60
        private Dictionary<Stats,decimal> getBaseStats(string json)
        {
            if (json == null)
            {
                return null;
            }
            JToken statsJson = JObject.Parse(json)["results"][0]["stats"];
            statsJson = lvl == 50 ? statsJson["lv50FiveStarNoAwaken"] : statsJson["lv60SixStarNoAwaken"];
            Dictionary<Stats,decimal> baseStats = new Dictionary<Stats, decimal>();
            var stats = statsJson.Children().GetEnumerator();
            stats.MoveNext();
            stats.MoveNext();
            do
            {  //skip CP
                JProperty stat = (JProperty)stats.Current;
                if (stat.Name.ToUpper() != "DAC")
                {
                    baseStats[(Stats)Enum.Parse(typeof(Stats), stat.Name.ToUpper().Replace("CHC", "Crit").Replace("CHD", "CritDmg").Replace("EFR", "RES"))] = (decimal)stat.Value;
                }
            } while (stats.MoveNext());
            return baseStats;
        }

        //Parse JSON data from EpicSevenDB to get the stats of an awakened hero
        private Dictionary<Stats,decimal> getAwakeningStats(string json)
        {
            JToken statsJson = JObject.Parse(json)["results"][0]["awakening"];
            Dictionary<Stats, decimal> awakeningStats = new Dictionary<Stats, decimal>();
            for (int i = 0; i < Awakening ;i++)
            {
                JToken stats = statsJson[i]["statsIncrease"];
                for (int j = 0; j < stats.Count(); j++)
                {
                    JProperty stat = (JProperty)stats[j].First;
                    string name = stat.Name.ToUpper();
                    Stat s;
                    if ((name == "ATK" || name == "HP" || name == "DEF") && (decimal)stat.Value < 1)
                    {
                        s = new Stat((Stats)Enum.Parse(typeof(Stats), stat.Name.ToUpper() + "Percent"), (decimal)stat.Value);
                    } else
                    {
                        s = new Stat((Stats)Enum.Parse(typeof(Stats), stat.Name.ToUpper().Replace("CHC", "Crit").Replace("CHD", "CritDmg").Replace("EFR", "RES")), (decimal)stat.Value);
                    }
                    if (awakeningStats.ContainsKey(s.Name))
                    {
                        awakeningStats[s.Name] += s.Value;
                    }
                    else
                    {
                        awakeningStats.Add(s.Name, s.Value);
                    }
                }
            }
            return awakeningStats;
        }


        public void calcAwakeningStats()
        {
            string json = Util.client.DownloadString(Util.ApiUrl + "/hero/" + Util.toAPIUrl(Name));
            AwakeningStats = getAwakeningStats(json);
            stars = getStars(lvl, awakening);
        }

        private Element getElement(string json)
        {
            JToken info = JObject.Parse(json)["results"][0];
            return (Element)Enum.Parse(typeof(Element), System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string)info["element"]));
        }

        private HeroClass getClass(string json)
        {
            JToken info = JObject.Parse(json)["results"][0];
            return (HeroClass)Enum.Parse(typeof(HeroClass), System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string)info["classType"]).Replace("Soul-Weaver","SoulWeaver"));
        }

        //Calculates the current stats of a hero
        public Dictionary<Stats, decimal> calcStats()
        {
            Dictionary<Stats, decimal> itemStats = new Dictionary<Stats, decimal>();
            foreach (Stats s in Enum.GetValues(typeof(Stats)))
            {
                itemStats[s] = 0;
            }
            foreach (Item item in gear.Values)
            {
                itemStats[item.Main.Name] += item.Main.Value;
                foreach (Stat s in item.SubStats)
                {
                    itemStats[s.Name] += s.Value;
                }
            }
            Dictionary<Stats, decimal> setBonusStats = this.setBonusStats();
            Dictionary<Stats, decimal> calculatedStats = new Dictionary<Stats, decimal>();
            calculatedStats[Stats.ATK] = (BaseStats[Stats.ATK] * (1 + (AwakeningStats.ContainsKey(Stats.ATKPercent) ? AwakeningStats[Stats.ATKPercent] : 0))) + (AwakeningStats.ContainsKey(Stats.ATK) ? AwakeningStats[Stats.ATK] : 0);
            calculatedStats[Stats.ATK] = (calculatedStats[Stats.ATK] * (1 + itemStats[Stats.ATKPercent] + setBonusStats[Stats.ATKPercent])) + itemStats[Stats.ATK] + Artifact.SubStats[0].Value;
            calculatedStats[Stats.HP] = (BaseStats[Stats.HP] * (1 + (AwakeningStats.ContainsKey(Stats.HPPercent) ? AwakeningStats[Stats.HPPercent] : 0))) + (AwakeningStats.ContainsKey(Stats.HP) ? AwakeningStats[Stats.HP] : 0);
            calculatedStats[Stats.HP] = (calculatedStats[Stats.HP] * (1 + itemStats[Stats.HPPercent] + setBonusStats[Stats.HPPercent])) + itemStats[Stats.HP] + Artifact.SubStats[1].Value;
            calculatedStats[Stats.DEF] = BaseStats[Stats.DEF] * (1 + (AwakeningStats.ContainsKey(Stats.DEFPercent) ? AwakeningStats[Stats.DEFPercent] : 0));
            calculatedStats[Stats.DEF] = (calculatedStats[Stats.DEF] * (1 + itemStats[Stats.DEFPercent] + setBonusStats[Stats.DEFPercent])) + itemStats[Stats.DEF];
            calculatedStats[Stats.SPD] = BaseStats[Stats.SPD] + (AwakeningStats.ContainsKey(Stats.SPD) ? AwakeningStats[Stats.SPD] : 0);
            calculatedStats[Stats.SPD] = (calculatedStats[Stats.SPD] * (1 + setBonusStats[Stats.SPD])) + itemStats[Stats.SPD];
            calculatedStats[Stats.Crit] = BaseStats[Stats.Crit] + (AwakeningStats.ContainsKey(Stats.Crit) ? AwakeningStats[Stats.Crit] : 0);
            calculatedStats[Stats.Crit] = calculatedStats[Stats.Crit] + itemStats[Stats.Crit] + setBonusStats[Stats.Crit];
            calculatedStats[Stats.Crit] = calculatedStats[Stats.Crit] > 1 ? 1 : calculatedStats[Stats.Crit];
            calculatedStats[Stats.CritDmg] = BaseStats[Stats.CritDmg] + (AwakeningStats.ContainsKey(Stats.CritDmg) ? AwakeningStats[Stats.CritDmg] : 0);
            calculatedStats[Stats.CritDmg] = calculatedStats[Stats.CritDmg] + itemStats[Stats.CritDmg] + setBonusStats[Stats.CritDmg];
            calculatedStats[Stats.EFF] = BaseStats[Stats.EFF] + (AwakeningStats.ContainsKey(Stats.EFF) ? AwakeningStats[Stats.EFF] : 0);
            calculatedStats[Stats.EFF] = calculatedStats[Stats.EFF] + itemStats[Stats.EFF] + setBonusStats[Stats.EFF];
            calculatedStats[Stats.RES] = BaseStats[Stats.RES] + (AwakeningStats.ContainsKey(Stats.RES) ? AwakeningStats[Stats.RES] : 0);
            calculatedStats[Stats.RES] = calculatedStats[Stats.RES] + itemStats[Stats.RES] + setBonusStats[Stats.RES];
            calculatedStats[Stats.EHP] = calculatedStats[Stats.HP] * (1 + (calculatedStats[Stats.DEF] / 300));
            calculatedStats[Stats.DMG] = (calculatedStats[Stats.ATK] * (1 - calculatedStats[Stats.Crit])) + (calculatedStats[Stats.ATK] * calculatedStats[Stats.Crit] * calculatedStats[Stats.CritDmg]);
            currentStats = calculatedStats;
            return calculatedStats;
        }

        //Calculates the stats of a hero with a given set of gear
        public Dictionary<Stats, decimal> calcStatsWithGear(List<Item> gear, out List<Set> aS, decimal critbonus)
        {
            Dictionary<Stats, decimal> itemStats = new Dictionary<Stats, decimal>();
            foreach (Stats s in Enum.GetValues(typeof(Stats)))
            {
                itemStats[s] = 0;
            }
            foreach (Item item in gear)
            {
                itemStats[item.Main.Name] += item.Main.Value;
                foreach (Stat s in item.SubStats)
                {
                    itemStats[s.Name] += s.Value;
                }
            }
            Dictionary<Stats, decimal> setBonusStats = this.setBonusStatsWithGear(gear, out aS);
            Dictionary<Stats, decimal> calculatedStats = new Dictionary<Stats, decimal>();
            calculatedStats[Stats.ATK] = (BaseStats[Stats.ATK] * (1 + (AwakeningStats.ContainsKey(Stats.ATKPercent) ? AwakeningStats[Stats.ATKPercent] : 0))) + AwakeningStats[Stats.ATK];
            calculatedStats[Stats.ATK] = (calculatedStats[Stats.ATK] * (1 + itemStats[Stats.ATKPercent] + setBonusStats[Stats.ATKPercent])) + itemStats[Stats.ATK] + Artifact.SubStats[0].Value;
            calculatedStats[Stats.HP] = (BaseStats[Stats.HP] * (1 + (AwakeningStats.ContainsKey(Stats.HPPercent) ? AwakeningStats[Stats.HPPercent] : 0))) + AwakeningStats[Stats.HP];
            calculatedStats[Stats.HP] = (calculatedStats[Stats.HP] * (1 + itemStats[Stats.HPPercent] + setBonusStats[Stats.HPPercent])) + itemStats[Stats.HP] + Artifact.SubStats[1].Value;
            calculatedStats[Stats.DEF] = BaseStats[Stats.DEF] * (1 + (AwakeningStats.ContainsKey(Stats.DEFPercent) ? AwakeningStats[Stats.DEFPercent] : 0));
            calculatedStats[Stats.DEF] = (calculatedStats[Stats.DEF] * (1 + itemStats[Stats.DEFPercent] + setBonusStats[Stats.DEFPercent])) + itemStats[Stats.DEF];
            calculatedStats[Stats.SPD] = BaseStats[Stats.SPD] + (AwakeningStats.ContainsKey(Stats.SPD) ? AwakeningStats[Stats.SPD] : 0);
            calculatedStats[Stats.SPD] = (calculatedStats[Stats.SPD] * (1 + setBonusStats[Stats.SPD])) + itemStats[Stats.SPD];
            calculatedStats[Stats.Crit] = BaseStats[Stats.Crit] + (AwakeningStats.ContainsKey(Stats.Crit) ? AwakeningStats[Stats.Crit] : 0);
            calculatedStats[Stats.Crit] = calculatedStats[Stats.Crit] + itemStats[Stats.Crit] + setBonusStats[Stats.Crit] + critbonus;
            calculatedStats[Stats.Crit] = calculatedStats[Stats.Crit] > 1 ? 1 : calculatedStats[Stats.Crit];
            calculatedStats[Stats.CritDmg] = BaseStats[Stats.CritDmg] + (AwakeningStats.ContainsKey(Stats.CritDmg) ? AwakeningStats[Stats.CritDmg] : 0);
            calculatedStats[Stats.CritDmg] = calculatedStats[Stats.CritDmg] + itemStats[Stats.CritDmg] + setBonusStats[Stats.CritDmg];
            calculatedStats[Stats.EFF] = BaseStats[Stats.EFF] + (AwakeningStats.ContainsKey(Stats.EFF) ? AwakeningStats[Stats.EFF] : 0);
            calculatedStats[Stats.EFF] = calculatedStats[Stats.EFF] + itemStats[Stats.EFF] + setBonusStats[Stats.EFF];
            calculatedStats[Stats.RES] = BaseStats[Stats.RES] + (AwakeningStats.ContainsKey(Stats.RES) ? AwakeningStats[Stats.RES] : 0);
            calculatedStats[Stats.RES] = calculatedStats[Stats.RES] + itemStats[Stats.RES] + setBonusStats[Stats.RES];
            calculatedStats[Stats.EHP] = calculatedStats[Stats.HP] * (1 + (calculatedStats[Stats.DEF] / 300));
            calculatedStats[Stats.DMG] = (calculatedStats[Stats.ATK] * (1 - calculatedStats[Stats.Crit])) + (calculatedStats[Stats.ATK] * calculatedStats[Stats.Crit] * calculatedStats[Stats.CritDmg]);
            return calculatedStats;
        }

        //Calculates the stats from set bonuses
        public Dictionary<Stats, decimal> setBonusStats()
        {
            List<Set> activeSets = this.activeSets();
            Dictionary<Stats, decimal> stats = new Dictionary<Stats, decimal>();
            foreach (Stats s in Enum.GetValues(typeof(Stats)))
            {
                stats[s] = 0;
            }
            foreach (Set set in activeSets)
            {
                switch (set)
                {
                    case Set.Attack:
                        stats[Stats.ATKPercent] += 0.35m;
                        break;
                    case Set.Crit:
                        stats[Stats.Crit] += 0.12m;
                        break;
                    case Set.Def:
                        stats[Stats.DEFPercent] += 0.15m;
                        break;
                    case Set.Destruction:
                        stats[Stats.CritDmg] += 0.4m;
                        break;
                    case Set.Health:
                        stats[Stats.HPPercent] += 0.15m;
                        break;
                    case Set.Hit:
                        stats[Stats.EFF] += 0.2m;
                        break;
                    case Set.Resist:
                        stats[Stats.RES] += 0.2m;
                        break;
                    case Set.Speed:
                        stats[Stats.SPD] += 0.25m;
                        break;
                    default:
                        break;
                }
            }
            return stats;
        }

        //Calculates the stats from set bonuses with a given set of gear
        public Dictionary<Stats, decimal> setBonusStatsWithGear(List<Item> gear, out List<Set> aS)
        {
            List<Set> activeSets = this.activeSetsWithGear(gear);
            Dictionary<Stats, decimal> stats = new Dictionary<Stats, decimal>();
            foreach (Stats s in Enum.GetValues(typeof(Stats)))
            {
                stats[s] = 0;
            }
            foreach (Set set in activeSets)
            {
                switch (set)
                {
                    case Set.Attack:
                        stats[Stats.ATKPercent] += 0.35m;
                        break;
                    case Set.Crit:
                        stats[Stats.Crit] += 0.12m;
                        break;
                    case Set.Def:
                        stats[Stats.DEFPercent] += 0.15m;
                        break;
                    case Set.Destruction:
                        stats[Stats.CritDmg] += 0.4m;
                        break;
                    case Set.Health:
                        stats[Stats.HPPercent] += 0.15m;
                        break;
                    case Set.Hit:
                        stats[Stats.EFF] += 0.2m;
                        break;
                    case Set.Resist:
                        stats[Stats.RES] += 0.2m;
                        break;
                    case Set.Speed:
                        stats[Stats.SPD] += 0.25m;
                        break;
                    default:
                        break;
                }
            }
            aS = activeSets;
            return stats;
        }

        public List<Set> activeSets()
        {
            return Util.activeSet(gear.Values.ToList());
        }

        public List<Set> activeSetsWithGear(List<Item> gear)
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

        public void unequip(Item item)
        {
            gear.Remove(item.Type);
            item.Equipped = null;
            calcStats();
        }

        public void unequipAll()
        {
            List<Item> temp = gear.Values.ToList();
            foreach (Item item in temp)
            {
                gear.Remove(item.Type);
                item.Equipped = null;
            }
            calcStats();
        }

        public void equip(List<Item> items)
        {
            foreach (Item item in items)
            {
                gear[item.Type] = item;
                item.Equipped = this;
            }
            calcStats();
        }

        public void equip(Item item)
        {
            gear[item.Type] = item;
            item.Equipped = this;
            calcStats();
        }

        public Item getItem(ItemType type)
        {
            return gear.ContainsKey(type) ? gear[type] : null;
        }

        public List<Item> getGear()
        {
            return gear.Values.ToList();
        }
    }
}
