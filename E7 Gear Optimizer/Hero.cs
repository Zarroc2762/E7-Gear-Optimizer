using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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

        private Dictionary<Stats, float> baseStats;
        public Dictionary<Stats, float> BaseStats { get => baseStats; }
        private Dictionary<Stats, float> currentStats;
        private Dictionary<Stats, float> awakeningStats;
        public Dictionary<Stats, float> AwakeningStats { get => awakeningStats; }

        //Cache of Enum.GetValues(typeof(Stats)). Used to iterate over Stats. Greatly increases performance.
        public static Stats[] statsArrayGeneric = Enum.GetValues(typeof(Stats)).Cast<Stats>().ToArray();

        public Skill[] Skills { get; }
        public Skill SkillWithSoulburn { get; }

        public Hero(string ID, string name, List<Item> gear, Item artifact, int lvl, int awakening, int[] skillEnhance = null)
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
            string json = loadJson();
            JObject jObject = JObject.Parse(json);
            baseStats = getBaseStats(json);
            Element = getElement(json);
            Class = getClass(json);
            awakeningStats = getAwakeningStats(json);
            Skills = new Skill[3];
            for (var iSkill = 0; iSkill < 3; iSkill++)
            {
                try
                {
                    Skills[iSkill] = new Skill(jObject, iSkill, skillEnhance != null ? skillEnhance[iSkill] : 0);
                }
                catch (Skill.UnsupportedDamageModifierException ex)
                {
                    Skills[iSkill] = new Skill();
#if DEBUG
                    MessageBox.Show(ex.Message + Environment.NewLine + "Hero: " + name, "Unsupported damage modifier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
                }
            };
            SkillWithSoulburn = Skills.FirstOrDefault(s => s.HasSoulburn) ?? new Skill();
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
        public Dictionary<Stats, float> CurrentStats { get => currentStats; }

        //Fetch the portrait of the hero from EpicSevenDB
        private Image getPortrait(string name)
        {
            Bitmap portrait;
            try
            {
                string cacheFileName = System.IO.Path.Combine("cache", $"db.hero.{Name}.icon.png");
                if (Properties.Settings.Default.UseCache && System.IO.File.Exists(cacheFileName))
                {
                    //using FileStream as the file is locked otherwise and cannot be deleted on cache invalidation
                    using (var fs = new FileStream(cacheFileName, System.IO.FileMode.Open))
                    {
                        portrait = new Bitmap(fs);
                    }
                }
                else
                {
                    portrait = new Bitmap(Util.client.OpenRead(Util.AssetUrl + "/hero/" + Util.toAPIUrl(Name) + "/icon.png"));
                    if (Properties.Settings.Default.UseCache)
                    {
                        portrait.Save(cacheFileName, ImageFormat.Png);
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError)
                {
                    MessageBox.Show(ex.Message);
                }
                else if (((HttpWebResponse)ex.Response).StatusCode != HttpStatusCode.NotFound)
                {
                    MessageBox.Show(ex.Message);
                }
                portrait = Util.error;
            }
            catch (Exception ex)
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
        private Dictionary<Stats,float> getBaseStats(string json)
        {
            if (json == null)
            {
                return null;
            }
            JToken statsJson = JObject.Parse(json)["results"][0]["calculatedStatus"];
            statsJson = lvl == 50 ? statsJson["lv50FiveStarNoAwaken"] : statsJson["lv60SixStarNoAwaken"];
            Dictionary<Stats,float> baseStats = new Dictionary<Stats, float>();
            var stats = statsJson.Children().GetEnumerator();
            stats.MoveNext();
            stats.MoveNext();
            do
            {  //skip CP
                JProperty stat = (JProperty)stats.Current;
                if (stat.Name.ToUpper() != "DAC")
                {
                    baseStats[(Stats)Enum.Parse(typeof(Stats), stat.Name.ToUpper().Replace("CHC", "Crit").Replace("CHD", "CritDmg").Replace("EFR", "RES"))] = (float)stat.Value;
                }
            } while (stats.MoveNext());
            return baseStats;
        }

        //Updates base stats from EpicSevenDB
        public void updateBaseStats()
        {
            string json = loadJson();
            json = Encoding.UTF8.GetString(Encoding.Default.GetBytes(json)).Replace("✰", "");
            json = json.Remove(json.IndexOf("\"skills\":")) + json.Substring(json.IndexOf("\"zodiac_tree\":"));
            baseStats = getBaseStats(json);
        }

        //Parse JSON data from EpicSevenDB to get the stats of an awakened hero
        private Dictionary<Stats,float> getAwakeningStats(string json)
        {
            JToken statsJson = JObject.Parse(json)["results"][0]["zodiac_tree"];
            Dictionary<Stats, float> awakeningStats = new Dictionary<Stats, float>();
            for (int i = 0; i < Awakening ;i++)
            {
                JToken stats = statsJson[i]["stats"];
                for (int j = 0; j < stats.Count(); j++)
                {
                    string name = stats[j]["stat"].ToString().ToUpper();

                    Stat s;
                    if ((name == "ATT_RATE" || name == "MAX_HP_RATE" || name == "DEF_RATE"))
                    {
                        s = new Stat((Stats)Enum.Parse(typeof(Stats), name.Replace("ATT_RATE", "ATKPercent").Replace("MAX_HP_RATE", "HPPercent").Replace("DEF_RATE", "DEFPercent")), (float)stats[j]["value"]);
                    } else
                    {
                        s = new Stat((Stats)Enum.Parse(typeof(Stats), name.ToUpper().Replace("ATT", "ATK").Replace("SPEED", "SPD").Replace("CRI", "Crit").Replace("CRI_DMG", "CritDmg").Replace("MAX_HP", "HP").Replace("ACC", "EFF")), (float)stats[j]["value"]);
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
            string json = loadJson();
            json = Encoding.UTF8.GetString(Encoding.Default.GetBytes(json)).Replace("✰", "");
            json = json.Remove(json.IndexOf("\"skills\":")) + json.Substring(json.IndexOf("\"zodiac_tree\":"));
            awakeningStats = getAwakeningStats(json);
            stars = getStars(lvl, awakening);
        }

        private Element getElement(string json)
        {
            JToken info = JObject.Parse(json)["results"][0];
            return (Element)Enum.Parse(typeof(Element), System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string)info["attribute"]).Replace("Wind", "Earth"));
        }

        private HeroClass getClass(string json)
        {
            JToken info = JObject.Parse(json)["results"][0];
            return (HeroClass)Enum.Parse(typeof(HeroClass), System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string)info["role"])
                .Replace("Manauser","SoulWeaver")
                .Replace("Assassin", "Thief"));
        }

        //Calculates the current stats of a hero
        public Dictionary<Stats, float> calcStats()
        {
            Dictionary<Stats, float> itemStats = new Dictionary<Stats, float>();
            foreach (Stats s in statsArrayGeneric)
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
            Dictionary<Stats, float> setBonusStats = this.setBonusStats();
            Dictionary<Stats, float> calculatedStats = new Dictionary<Stats, float>();
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
            calculatedStats[Stats.HPpS] = calculatedStats[Stats.HP] * calculatedStats[Stats.SPD] / 100;
            calculatedStats[Stats.EHPpS] = calculatedStats[Stats.EHP] * calculatedStats[Stats.SPD] / 100;
            calculatedStats[Stats.DMGpS] = calculatedStats[Stats.DMG] * calculatedStats[Stats.SPD] / 100;
            currentStats = calculatedStats;
            return calculatedStats;
        }

        public Dictionary<Stats, float> calcStatsWithoutGear(float critbonus)
        {
            Dictionary<Stats, float> stats = new Dictionary<Stats, float>();
            foreach (Stats s in statsArrayGeneric)
            {
                stats[s] = 0;
            }
            stats[Stats.ATK] = (BaseStats[Stats.ATK] * (1 + (AwakeningStats.ContainsKey(Stats.ATKPercent) ? AwakeningStats[Stats.ATKPercent] : 0))) + (AwakeningStats.ContainsKey(Stats.ATK) ? AwakeningStats[Stats.ATK] : 0);
            stats[Stats.HP] = (BaseStats[Stats.HP] * (1 + (AwakeningStats.ContainsKey(Stats.HPPercent) ? AwakeningStats[Stats.HPPercent] : 0))) + (AwakeningStats.ContainsKey(Stats.HP) ? AwakeningStats[Stats.HP] : 0);
            stats[Stats.DEF] = BaseStats[Stats.DEF] * (1 + (AwakeningStats.ContainsKey(Stats.DEFPercent) ? AwakeningStats[Stats.DEFPercent] : 0));
            stats[Stats.SPD] = BaseStats[Stats.SPD] + (AwakeningStats.ContainsKey(Stats.SPD) ? AwakeningStats[Stats.SPD] : 0);
            stats[Stats.Crit] = BaseStats[Stats.Crit] + (AwakeningStats.ContainsKey(Stats.Crit) ? AwakeningStats[Stats.Crit] : 0) + critbonus;
            stats[Stats.CritDmg] = BaseStats[Stats.CritDmg] + (AwakeningStats.ContainsKey(Stats.CritDmg) ? AwakeningStats[Stats.CritDmg] : 0);
            stats[Stats.EFF] = BaseStats[Stats.EFF] + (AwakeningStats.ContainsKey(Stats.EFF) ? AwakeningStats[Stats.EFF] : 0);
            stats[Stats.RES] = BaseStats[Stats.RES] + (AwakeningStats.ContainsKey(Stats.RES) ? AwakeningStats[Stats.RES] : 0);
            return stats;
        }

        //Calculates the stats from set bonuses
        public Dictionary<Stats, float> setBonusStats()
        {
            List<Set> activeSets = this.activeSets();
            Dictionary<Stats, float> stats = new Dictionary<Stats, float>();
            foreach (Stats s in statsArrayGeneric)
            {
                stats[s] = 0;
            }
            foreach (Set set in activeSets)
            {
                switch (set)
                {
                    case Set.Attack:
                        stats[Stats.ATKPercent] += 0.35f;
                        break;
                    case Set.Crit:
                        stats[Stats.Crit] += 0.12f;
                        break;
                    case Set.Def:
                        stats[Stats.DEFPercent] += 0.15f;
                        break;
                    case Set.Destruction:
                        stats[Stats.CritDmg] += 0.4f;
                        break;
                    case Set.Health:
                        stats[Stats.HPPercent] += 0.15f;
                        break;
                    case Set.Hit:
                        stats[Stats.EFF] += 0.2f;
                        break;
                    case Set.Resist:
                        stats[Stats.RES] += 0.2f;
                        break;
                    case Set.Speed:
                        stats[Stats.SPD] += 0.25f;
                        break;
                    default:
                        break;
                }
            }
            return stats;
        }

        //Calculates the stats from set bonuses with a given set of gear
        public SStats setBonusStats(List<Set> activeSets)
        {
            SStats stats = new SStats();
            foreach (Set set in activeSets)
            {
                switch (set)
                {
                    case Set.Attack:
                        stats.ATKPercent += 0.35f;
                        break;
                    case Set.Crit:
                        stats.Crit += 0.12f;
                        break;
                    case Set.Def:
                        stats.DEFPercent += 0.15f;
                        break;
                    case Set.Destruction:
                        stats.CritDmg += 0.4f;
                        break;
                    case Set.Health:
                        stats.HPPercent += 0.15f;
                        break;
                    case Set.Hit:
                        stats.EFF += 0.2f;
                        break;
                    case Set.Resist:
                        stats.RES += 0.2f;
                        break;
                    case Set.Speed:
                        stats.SPD += 0.25f;
                        break;
                    default:
                        break;
                }
            }
            return stats;
        }

        public List<Set> activeSets()
        {
            return Util.activeSet(gear.Values.ToList());
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
                if (gear.ContainsKey(item.Type))
                {
                    gear[item.Type].Equipped = null;
                }
                gear[item.Type] = item;
                item.Equipped = this;
            }
            calcStats();
        }

        public void equip(Item item)
        {
            if (gear.ContainsKey(item.Type))
            {
                gear[item.Type].Equipped = null;
            }
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

        private string loadJson()
        {
            string cacheFileName = System.IO.Path.Combine(Properties.Settings.Default.CacheDirectory, $"db.hero.{Name}.json");
            string json = null;
            if (Properties.Settings.Default.UseCache && File.Exists(cacheFileName) && System.DateTime.Now.Subtract(File.GetLastWriteTime(cacheFileName)).TotalDays <= Properties.Settings.Default.CacheTimeToLive)
            {
                json = File.ReadAllText(cacheFileName);
            }
            else
            {
                try
                {
                    json = Util.client.DownloadString(Util.ApiUrl + "/hero/" + Util.toAPIUrl(Name));
                    if (Properties.Settings.Default.UseCache)
                    {
                        File.WriteAllText(cacheFileName, json);
                    }
                }
                catch (WebException ex)
                {
                    MessageBox.Show("Could not connect to epicsevendb.com. Please check your internet connection.\nThe Optimizer will try to use a cached version of epicsevendb.com if it is available.");
                    if (File.Exists(cacheFileName))
                    {
                        json = File.ReadAllText(cacheFileName);
                    }
                }
            }
            //remove text which results in json errors on systems with some specific language settings
            json = Encoding.UTF8.GetString(Encoding.Default.GetBytes(json)).Replace("✰", "");
            json = System.Text.RegularExpressions.Regex.Replace(json, "\"Shining Star[^\"]*\"", "\"\"");
            json = System.Text.RegularExpressions.Regex.Replace(json, "description\":\"[^\"]*\",\"enhancement", "description\":\"\",\"enhancement");
            return json;
        }
    }
}
