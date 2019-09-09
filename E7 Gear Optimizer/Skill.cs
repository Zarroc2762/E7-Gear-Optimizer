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

        public Skill(JObject jObject, int index, int enhanceLevel = 0)
        {
            try
            {
                JToken jSkill = jObject["results"][0]["skills"][index];
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
                        default:
                            //throw new ArgumentOutOfRangeException(modifier.name);
                            break;
                    }
                }
                jEnhancement = jSkill["enhancement"].ToArray();
                Enhance = enhanceLevel;
                HasSoulburn = jSkill["soulBurn"].ToObject<int>() > 0;
            }
            catch
            {
                pow = 0;
                powSoul = 0;
                atk = 0;
                atkSoul = 0;
                spd = 0;
                spdSoul = 0;
                def = 0;
                defSoul = 0;
                hp = 0;
                hpSoul = 0;
                critDmg = 1;
                critDmgSoul = 1;
                damageIncrease = 1;
                //throw;
            }
        }

        JToken[] jEnhancement;

        int enhanceLevel;

        public int Enhance
        {
            get => enhanceLevel;
            set
            {
                enhanceLevel = value;
                DamageIncrease = 0;
                if (jEnhancement != null)
                {
                    if (enhanceLevel > jEnhancement.Length)
                    {
                        enhanceLevel = jEnhancement.Length;
                    }
                    for (int i = 0; i < enhanceLevel; i++)
                    {
                        string desc = jEnhancement[i]["description"].ToString();
                        var match = enhancementDescRegex.Match(desc);
                        if (match.Success)
                        {
                            DamageIncrease += int.Parse(match.Groups[1].Value);
                        }
                    }
                }
                damageIncrease = 1 + DamageIncrease / 100;
            }
        }

        

        public bool HasSoulburn { get; }
        public int DamageIncrease { get; private set; }

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

        /// <summary>
        /// Calculate damage of the skill based on SStats of the hero
        /// </summary>
        /// <param name="stats">SStats of the hero or calulation result</param>
        /// <param name="crit">Some skills have increased critical damage</param>
        /// <param name="soulburn">Is soulburn used?</param>
        /// <param name="enemyDef">Enemy target's defence.
        /// Slimes in 1-1 have 55. Wyvern 1 wave 1 dragons have 165.
        /// The Mossy Testudos in Golem 6 have 642 Defense.
        /// The Blaze Dragonas in Wyvern 6 have 592 Defense.
        /// </param>
        /// <returns></returns>
        public float CalcDamage(SStats stats, bool crit = false, bool soulburn = false, int enemyDef = 0)
        {
            float dmg;
            if (HasSoulburn && soulburn)
            {
                dmg = (atkSoul * stats.ATK + hpSoul * stats.HP + defSoul * stats.DEF) * (1 + spdSoul * stats.SPD) * powSoul;
                if (crit)
                {
                    dmg *= critDmgSoul * stats.CritDmg;
                }
            }
            else
            { 
                dmg = (atk * stats.ATK + hp * stats.HP + def * stats.DEF) * (1 + spd * stats.SPD) * pow;
                if (crit)
                {
                    dmg *= critDmg * stats.CritDmg;
                }
            }
            dmg *= POW_CONST * damageIncrease;
            if (enemyDef == 0)
            {
                return dmg;
            }
            else
            {
                return dmg / (enemyDef / 300f + 1);
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
