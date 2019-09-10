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

        /// <summary>
        /// Initializes abstract skill which damage is always equals to 0
        /// </summary>
        public Skill()
        {
        }

        /// <summary>
        /// Initializes new instance of a <see cref="Skill"/>
        /// </summary>
        /// <param name="jObject">JObject with the hero's parsed JSON from epicsevendb.com to read multipliers from</param>
        /// <param name="index">Zero-based index of the skill</param>
        /// <param name="enhanceLevel">Enhance level of the hero's skill</param>
        public Skill(JObject jObject, int index, int enhanceLevel = 0)
        {
            JToken jSkill = jObject["results"][0]["skills"][index];
            JToken jDamageModifiers = jSkill["damageModifiers"];
            foreach (var jModifier in jDamageModifiers)
            {
                string name = jModifier["name"].ToString();
                float value;
                float soulburn;
                var jValue = jModifier["value"];
                if (jValue is JArray)
                {
                    //some values stored as arrays e.g. BBK's, as her damage scales with lost health
                    //in such cases calc damage using only the first value
                    value = ((JArray)jValue).First().Value<float>();
                }
                else
                {
                    value = jValue.Value<float>();
                }
                var jSoulburn = jModifier["soulburn"];
                if (jSoulburn is JArray)
                {
                    soulburn = ((JArray)jSoulburn).First().Value<float>();
                }
                else
                {
                    soulburn = jSoulburn.Value<float>();
                }
                switch (name)
                {
                    case "pow":
                        pow = value;
                        powSoul = soulburn;
                        break;
                    case "atk_rate":
                        atk = value;
                        atkSoul = soulburn;
                        break;
                    case "hp_rate":
                        hp = value;
                        hpSoul = soulburn;
                        break;
                    case "def_rate":
                        def = value;
                        defSoul = soulburn;
                        break;
                    case "spd_rate":
                        spd = value;
                        spdSoul = soulburn;
                        break;
                    case "crit_dmg_rate":
                        critDmg = 1 + value;
                        critDmgSoul = 1 + soulburn;
                        break;
                    default:
                        throw new UnsupportedDamageModifierException(name);
                }
            }
            jEnhancement = jSkill["enhancement"].ToArray();
            Enhance = enhanceLevel;
            HasSoulburn = jSkill["soulBurn"].ToObject<int>() > 0;
        }

        /// <summary>
        /// Collection of 'enhancement' JTokens to use in case of enhanceLevel's change 
        /// </summary>
        JToken[] jEnhancement;

        int enhanceLevel;

        /// <summary>
        /// Gets or sets the skill's Enhance level
        /// </summary>
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
        /// <summary>
        /// Summary damage increase from skill's enhancements in %
        /// </summary>
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
        /// Calculate damage of the skill based on <see cref="SStats"/> of the hero
        /// </summary>
        /// <param name="stats"><see cref="SStats"/> of the hero or calulation result</param>
        /// <param name="crit">Some skills have increased critical damage</param>
        /// <param name="soulburn">Is soulburn used?</param>
        /// <param name="enemyDef">Enemy target's defence.</param>
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

        public class UnsupportedDamageModifierException : Exception
        {
            public UnsupportedDamageModifierException(string modifierName) : base($"Unsupported damage modifier: {modifierName}")
            {
                ModifierName = modifierName;
            }

            public string ModifierName { get; }
        }
    }
}
