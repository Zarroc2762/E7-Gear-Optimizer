using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace E7_Gear_Optimizer
{
    public class Data
    {
        public List<Item> Items { get; }
        public List<Hero> Heroes { get; }

        private string currentItemID;
        private string currentHeroID;

        public string CurrentItemID { get => currentItemID; }
        public string CurrentHeroID { get => currentHeroID; }
        public Data()
        {
            Items = new List<Item>();
            Heroes = new List<Hero>();
            currentItemID = "0";
            currentHeroID = "0";
        }

        //Parses the JSON data of the web Optimizer written by /u/HyrTheWinter
        public (bool, int , int) importFromWeb(string path, IProgress<int> progress, bool append)
        {
            if (!append)
            {
                Items.Clear();
                Heroes.Clear();
            }
            int oldHeroCount = Heroes.Count;
            int oldItemCount = Items.Count;
            string json = File.ReadAllText(path);
            Dictionary<string, string> IDConverter = new Dictionary<string, string>();
            try
            {
                JToken items = JObject.Parse(json)["items"];
                int length = items.Count();
                for (int i = 0; i < length; i++)
                {
                    JToken item = items[i];
                    int enhance;
                    enhance = item.Value<int>("ability");
                    int ilvl = item.Value<int>("level");
                    Set set = (Set)Enum.Parse(typeof(Set), item.Value<string>("set").Replace("Critical", "Crit").Replace("Defense", "Def"));
                    ItemType type = (ItemType)Enum.Parse(typeof(ItemType), item.Value<string>("slot"));
                    Grade grade = (Grade)Enum.Parse(typeof(Grade), item.Value<string>("rarity"));
                    JToken mainStat = item["mainStat"];
                    Stats main = (Stats)Enum.Parse(typeof(Stats), mainStat.Value<string>(0).ToUpper().Replace("ATKP", "ATKPercent").Replace("HPP", "HPPercent").Replace("DEFP", "DEFPercent").Replace("CCHANCE", "Crit").Replace("CDMG", "CritDmg"));
                    float stat = Util.percentStats.Contains(main) ? mainStat.Value<float>(1) / 100f : mainStat.Value<float>(1);
                    Stat main_Stat = new Stat(main, stat);
                    bool locked = item.Value<bool>("locked");
                    List<Stat> subStats = new List<Stat>();
                    for (int j = 1; j < 5; j++)
                    {
                        JToken subStat = item["subStat" + j.ToString()];
                        if (subStat != null)
                        {
                            try
                            {
                                Stats name = (Stats)Enum.Parse(typeof(Stats), subStat.Value<string>(0).ToUpper().Replace("ATKP", "ATKPercent").Replace("HPP", "HPPercent").Replace("DEFP", "DEFPercent").Replace("CCHANCE", "Crit").Replace("CDMG", "CritDmg"));
                                float value = Util.percentStats.Contains(name) ? subStat.Value<float>(1) / 100f : subStat.Value<float>(1);
                                if (value > 0)
                                {
                                    subStats.Add(new Stat(name, value));
                                }
                            }
                            catch
                            { }
                        }
                    }
                    string id = incrementItemID();
                    Items.Add(new Item(id, type, set, grade, ilvl, enhance, main_Stat, subStats.ToArray(), null, locked));
                    IDConverter.Add(item.Value<string>("id"), id);
                    if (length == 1)
                    {
                        progress.Report(100);
                    }
                    else
                    {
                        progress.Report((int)((float)i / ((float)length - 1) * 100));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return (false, 0, 0);
            }
            try
            {
                JToken heroes = JObject.Parse(json)["heroes"];
                int length = heroes.Count();
                for (int i = 0; i < length; i++)
                {
                    JToken hero = heroes[i];
                    JToken artifactStats = hero["artifactStats"];
                    float atk = artifactStats.Value<float>("atk");
                    float hp = artifactStats.Value<float>("hp");
                    Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, atk), new Stat(Stats.HP, hp) }, null, false);
                    string[] nameParts = hero.Value<string>("name").Split(' ');
                    string name = "";
                    for (int k = 0; k < nameParts.Length - 1; k++)
                    {
                        name += " " + nameParts[k];
                    }
                    name = name.Remove(0, 1);
                    int lvl = hero.Value<int>("level");
                    int awakening = hero.Value<int>("awakened");
                    JToken gear = hero["equipment"];
                    List<string> gearIDs = new List<string>();
                    if (gear.Value<string>("armorId") != null)
                    {
                        gearIDs.Add(gear.Value<string>("armorId"));
                    }
                    if (gear.Value<string>("helmetId") != null)
                    {
                        gearIDs.Add(gear.Value<string>("helmetId"));
                    }
                    if (gear.Value<string>("weaponId") != null)
                    {
                        gearIDs.Add(gear.Value<string>("weaponId"));
                    }
                    if (gear.Value<string>("necklaceId") != null)
                    {
                        gearIDs.Add(gear.Value<string>("necklaceId"));
                    }
                    if (gear.Value<string>("ringId") != null)
                    {
                        gearIDs.Add(gear.Value<string>("ringId"));
                    }
                    if (gear.Value<string>("bootsId") != null)
                    {
                        gearIDs.Add(gear.Value<string>("bootsId"));
                    }
                    List<Item> gearList = new List<Item>();
                    foreach (string id in gearIDs)
                    {
                        gearList.Add(Items.Find(x => x.ID == IDConverter[id]));
                    }
                    Hero newHero = new Hero(incrementHeroID(), name, gearList, artifact, lvl, awakening);
                    Heroes.Add(newHero);
                    if (length == 1)
                    {
                        progress.Report(100);
                    }
                    else
                    {
                        progress.Report((int)((float)i / ((float)length - 1) * 100));
                    }
                    if (i > 0 && i % 45 == 0)
                    {
                        Thread.Sleep(30000);
                    }
                }
                return (true, Heroes.Count - oldHeroCount, Items.Count - oldItemCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return (false, 0, 0);
            }
        }

        //Parses the JSON data generated by this program
        public (bool, int, int) importFromThis(string path, IProgress<int> progress, bool append)
        {
            if (!append)
            {
                Heroes.Clear();
                Items.Clear();
            }
            int oldHeroCount = Heroes.Count;
            int oldItemCount = Items.Count;
            Dictionary<string, string> IDConverter = new Dictionary<string, string>();
            string json = File.ReadAllText(path);
            try
            {
                JToken items = JObject.Parse(json)["items"];
                int length = items.Count();
                for (int i = 0; i < length; i++)
                {
                    JToken item = items[i];
                    string id = incrementItemID();
                    IDConverter.Add(item.Value<string>("ID"), id);
                    ItemType type = (ItemType)item.Value<int>("Type");
                    Set set = (Set)item.Value<int>("Set");
                    Grade grade = (Grade)item.Value<int>("Grade");
                    int ilvl = item.Value<int>("Ilvl");
                    int enhance = item.Value<int>("Enhance");
                    Stat mainStat = new Stat((Stats)item["Main"].Value<int>("Name"), item["Main"].Value<float>("Value"));
                    JToken subStats = item["SubStats"];
                    int subLength = subStats.Count();
                    List<Stat> subs = new List<Stat>();
                    for (int j = 0; j < subLength; j++)
                    {
                        subs.Add(new Stat((Stats)subStats[j].Value<int>("Name"), subStats[j].Value<float>("Value")));
                    }
                    bool locked = item.Value<bool>("Locked");
                    Items.Add(new Item(id, type, set, grade, ilvl, enhance, mainStat, subs.ToArray(), null, locked));
                    if (length == 1)
                    {
                        progress.Report(100);
                    }
                    else
                    {
                        progress.Report((int)((float)i / ((float)length - 1) * 100));
                    }
                }
                JToken heroes = JObject.Parse(json)["heroes"];
                length = heroes.Count();
                for (int i = 0; i < length; i++)
                {
                    JToken hero = heroes[i];
                    string id = incrementHeroID();
                    string name = hero.Value<string>("Name");
                    JToken gear = hero["Gear"];
                    int gearLength = gear.Count();
                    List<Item> gearList = new List<Item>();
                    for (int j = 0; j < gearLength; j++)
                    {
                        gearList.Add(Items.Find(x => x.ID == IDConverter[gear[j].Value<string>("ID")]));
                    }
                    Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, hero["Artifact"].Value<float>("ATK")), new Stat(Stats.HP, hero["Artifact"].Value<float>("HP")) }, null, false);
                    int lvl = hero.Value<int>("Lvl");
                    int awakening = hero.Value<int>("Awakening");
                    List<int> skillEnhance = new List<int>();
                    var jSkills = hero["Skills"]?.ToArray();
                    if (jSkills != null)
                    {
                        foreach (var jSkill in jSkills)
                        {
                            skillEnhance.Add(jSkill.Value<int>("Enhance"));
                        }
                    }
                    Heroes.Add(new Hero(id, name, gearList, artifact, lvl, awakening, skillEnhance.Count == 3 ? skillEnhance.ToArray() : null));
                    if (length == 1)
                    {
                        progress.Report(100);
                    }
                    else
                    {
                        progress.Report((int)((float)i / ((float)length - 1) * 100));
                    }
                    if (i > 0 && i % 45 == 0)
                    {
                        Thread.Sleep(30000);
                    }
                }
                JToken IDs = JObject.Parse(json);
                //currentItemID = IDs.Value<string>("currentItemID");
                //currentHeroID = IDs.Value<string>("currentHeroID");
                return (true, Heroes.Count - oldHeroCount, Items.Count - oldItemCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return (false, 0, 0);
            }
        }

        public string incrementItemID()
        {
            int ascii = currentItemID.Last();
            if (ascii == 122)
            {
                currentItemID += (char)65;
            }
            else
            {
                currentItemID = currentItemID.Substring(0, currentItemID.Length - 1) + (char)(ascii + 1);
            }
            return currentItemID;
        }
        public string incrementHeroID()
        {
            int ascii = currentHeroID.Last();
            if (ascii == 122)
            {
                currentHeroID += (char)65;
            }
            else
            {
                currentHeroID = currentHeroID.Substring(0, currentHeroID.Length - 1) + (char)(ascii + 1);
            }
            return currentHeroID;
        }

        
    }
}
