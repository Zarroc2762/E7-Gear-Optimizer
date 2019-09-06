using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace E7_Gear_Optimizer
{
    public class Skill
    {
        const float POW_CONST = 1.871f;

        static Regex enhancementDescRegex = new Regex(@"\+(\d+)% damage", RegexOptions.Compiled);

        public Skill(JToken json, int index, int enhanceLevel)
        {
            JToken jSkill = json["results"][0]["skills"][index];
            JToken jDamageModifiers = jSkill["damageModifiers"];
            foreach (var jModifier in jDamageModifiers)
            {
                var modifier = jModifier.ToObject<DamageModifier>();
                switch (modifier.name)
                {
                    case "pow":
                        pow = modifier.value;
                        powSoul = modifier.soulburn;
                        break;
                    case "atk_rate":
                        atk = modifier.value;
                        atkSoul = modifier.soulburn;
                        break;
                    case "hp_rate":
                        hp = modifier.value;
                        hpSoul = modifier.soulburn;
                        break;
                    case "def_rate":
                        def = modifier.value;
                        defSoul = modifier.soulburn;
                        break;
                    case "spd_rate":
                        spd = modifier.value;
                        spdSoul = modifier.soulburn;
                        break;
                    case "crit_dmg_rate":
                        critDmg = 1 + modifier.value;
                        critDmgSoul = 1 + modifier.soulburn;
                        break;
                }
            }
            var jEnhancement = jSkill["enhancement"].ToArray();
            for (int i = 0; i <= enhanceLevel && i < jEnhancement.Length; i++)
            {
                string desc = jEnhancement[i]["description"].ToString();
                var match = enhancementDescRegex.Match(desc);
                if (match.Success)
                {
                    damageIncrease += float.Parse(match.Groups[1].Value) / 100;
                }
            }

            //Dictionary<Stats, float> baseStats = new Dictionary<Stats, float>();
            //var stats = statsJson.Children().GetEnumerator();
            //stats.MoveNext();
            //stats.MoveNext();
            //do
            //{  //skip CP
            //    JProperty stat = (JProperty)stats.Current;
            //    if (stat.Name.ToUpper() != "DAC")
            //    {
            //        baseStats[(Stats)Enum.Parse(typeof(Stats), stat.Name.ToUpper().Replace("CHC", "Crit").Replace("CHD", "CritDmg").Replace("EFR", "RES"))] = (float)stat.Value;
            //    }
            //} while (stats.MoveNext());
            //return baseStats;


            //damageFunc = getDamageFunc(jDamageModifiers);
        }

        public int Enhance { get; set; }

        bool hasSoulburn;

        float pow;
        float powSoul;
        float atk;
        float atkSoul;
        float spd;
        float spdSoul;
        float def;
        float defSoul;
        float hp;
        float hpSoul;
        float critDmg = 1;
        float critDmgSoul = 1;
        float damageIncrease = 1;

        //The Mossy Testudos in Golem 6 have 642 Defense.
        //The Blaze Dragonas in Wyvern 6 have 592 Defense.
        //enemyDef is enemy defense. Slimes in 1-1 have 55. Wyvern 1 wave 1 dragons have 165
        float calcDamage(SStats stats, bool crit = false, bool soulburn = false, int enemyDef = 0)
        {
            float dmg;
            if (hasSoulburn && soulburn)
            {
                dmg = (atkSoul * stats.ATK + hpSoul * stats.HP + defSoul * stats.DEF) * (1 + spdSoul * stats.SPD) * powSoul;
                if (crit)
                {
                    dmg *= critDmgSoul;
                }
            }
            else
            { 
                dmg = (atk * stats.ATK + hp * stats.HP + def * stats.DEF) * (1 + spd * stats.SPD) * pow;
                if (crit)
                {
                    dmg *= critDmg;
                }
            }
            dmg *= POW_CONST * damageIncrease;
            if (enemyDef == 0)
            {
                return dmg;
            }
            else
            {
                return dmg / (enemyDef / 300 + 1);
            }
        }

        private struct DamageModifier
        {
            public string name;
            public float value;
            public float soulburn;
        }
    }
}
