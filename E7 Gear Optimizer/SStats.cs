using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E7_Gear_Optimizer
{
    public class SStats
    {
        public SStats()
        {

        }

        public SStats(Dictionary<Stats, float> stats)
        {
            ATKPercent = stats.ContainsKey(Stats.ATKPercent) ? stats[Stats.ATKPercent] : 0;
            ATK = stats.ContainsKey(Stats.ATK) ? stats[Stats.ATK] : 0;
            SPD = stats.ContainsKey(Stats.SPD) ? stats[Stats.SPD] : 0;
            Crit = stats.ContainsKey(Stats.Crit) ? stats[Stats.Crit] : 0;
            CritDmg = stats.ContainsKey(Stats.CritDmg) ? stats[Stats.CritDmg] : 0;
            HPPercent = stats.ContainsKey(Stats.HPPercent) ? stats[Stats.HPPercent] : 0;
            HP = stats.ContainsKey(Stats.HP) ? stats[Stats.HP] : 0;
            DEFPercent = stats.ContainsKey(Stats.DEFPercent) ? stats[Stats.DEFPercent] : 0;
            DEF = stats.ContainsKey(Stats.DEF) ? stats[Stats.DEF] : 0;
            EFF = stats.ContainsKey(Stats.EFF) ? stats[Stats.EFF] : 0;
            RES = stats.ContainsKey(Stats.RES) ? stats[Stats.RES] : 0;
            HPpS = stats.ContainsKey(Stats.HPpS) ? stats[Stats.HPpS] : 0;
            EHP = stats.ContainsKey(Stats.EHP) ? stats[Stats.EHP] : 0;
            EHPpS = stats.ContainsKey(Stats.EHPpS) ? stats[Stats.EHPpS] : 0;
            DMG = stats.ContainsKey(Stats.DMG) ? stats[Stats.DMG] : 0;
            DMGpS = stats.ContainsKey(Stats.DMGpS) ? stats[Stats.DMGpS] : 0;
        }

        public SStats(SStats sStats)
        {
            ATKPercent = sStats.ATKPercent;
            ATK = sStats.ATK;
            SPD = sStats.SPD;
            Crit = sStats.Crit;
            CritDmg = sStats.CritDmg;
            HPPercent = sStats.HPPercent;
            HP = sStats.HP;
            DEFPercent = sStats.DEFPercent;
            DEF = sStats.DEF;
            EFF = sStats.EFF;
            RES = sStats.RES;
            HPpS = sStats.HPpS;
            EHP = sStats.EHP;
            EHPpS = sStats.EHPpS;
            DMG = sStats.DMG;
            DMGpS = sStats.DMGpS;
        }

        public float ATKPercent;
        public float ATK;
        public float SPD;
        public float Crit;
        public float CritDmg;
        public float HPPercent;
        public float HP;
        public float DEFPercent;
        public float DEF;
        public float EFF;
        public float RES;
        public float HPpS;
        public float EHP;
        public float EHPpS;
        public float DMG;
        public float DMGpS;

        public void AddStatsValues(Stat[] stats)
        {
            foreach (var stat in stats)
            {
                switch (stat.Name)
                {
                    case Stats.ATKPercent:
                        ATKPercent += stat.Value;
                        break;
                    case Stats.ATK:
                        ATK += stat.Value;
                        break;
                    case Stats.SPD:
                        SPD += stat.Value;
                        break;
                    case Stats.Crit:
                        Crit += stat.Value;
                        break;
                    case Stats.CritDmg:
                        CritDmg += stat.Value;
                        break;
                    case Stats.HPPercent:
                        HPPercent += stat.Value;
                        break;
                    case Stats.HP:
                        HP += stat.Value;
                        break;
                    case Stats.DEFPercent:
                        DEFPercent += stat.Value;
                        break;
                    case Stats.DEF:
                        DEF += stat.Value;
                        break;
                    case Stats.EFF:
                        EFF += stat.Value;
                        break;
                    case Stats.RES:
                        RES += stat.Value;
                        break;
                    case Stats.HPpS:
                        HPpS += stat.Value;
                        break;
                    case Stats.EHP:
                        EHP += stat.Value;
                        break;
                    case Stats.EHPpS:
                        EHPpS += stat.Value;
                        break;
                    case Stats.DMG:
                        DMG += stat.Value;
                        break;
                    case Stats.DMGpS:
                        DMGpS += stat.Value;
                        break;
                }
            }
        }

        public void SubtractStatsValues(Stat[] stats)
        {
            foreach (var stat in stats)
            {
                switch (stat.Name)
                {
                    case Stats.ATKPercent:
                        ATKPercent -= stat.Value;
                        break;
                    case Stats.ATK:
                        ATK -= stat.Value;
                        break;
                    case Stats.SPD:
                        SPD -= stat.Value;
                        break;
                    case Stats.Crit:
                        Crit -= stat.Value;
                        break;
                    case Stats.CritDmg:
                        CritDmg -= stat.Value;
                        break;
                    case Stats.HPPercent:
                        HPPercent -= stat.Value;
                        break;
                    case Stats.HP:
                        HP -= stat.Value;
                        break;
                    case Stats.DEFPercent:
                        DEFPercent -= stat.Value;
                        break;
                    case Stats.DEF:
                        DEF -= stat.Value;
                        break;
                    case Stats.EFF:
                        EFF -= stat.Value;
                        break;
                    case Stats.RES:
                        RES -= stat.Value;
                        break;
                    case Stats.HPpS:
                        HPpS -= stat.Value;
                        break;
                    case Stats.EHP:
                        EHP -= stat.Value;
                        break;
                    case Stats.EHPpS:
                        EHPpS -= stat.Value;
                        break;
                    case Stats.DMG:
                        DMG -= stat.Value;
                        break;
                    case Stats.DMGpS:
                        DMGpS -= stat.Value;
                        break;
                }
            }
        }
    }
}
