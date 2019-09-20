using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E7_Gear_Optimizer
{
    /// <summary>
    /// Represents all stats of a character or an item
    /// </summary>
    public class SStats
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SStats"/> with all properties equal to zero
        /// </summary>
        public SStats()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SStats"/> with values copied from <paramref name="stats"/> Dictionary
        /// </summary>
        /// <param name="stats"></param>
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SStats"/> with values copied from another <see cref="SStats"/>
        /// </summary>
        /// <param name="stats"></param>
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
        }

        public float ATKPercent { get; set; }
        public float ATK { get; set; }
        public float SPD { get; set; }
        public float Crit { get; set; }
        public float CritCapped { get => Crit < 1 ? Crit : 1; }
        public float CritDmg { get; set; }
        public float HPPercent { get; set; }
        public float HP { get; set; }
        public float DEFPercent { get; set; }
        public float DEF { get; set; }
        public float EFF { get; set; }
        public float RES { get; set; }
        public float HPpS { get => HP * SPD / 100; }
        public float EHP { get => HP * (1 + (DEF / 300)); }
        public float EHPpS { get => EHP * SPD / 100; }
        public float DMG { get => (ATK * (1 - CritCapped)) + (ATK * CritCapped * CritDmg); }
        public float DMGpS { get => DMG * SPD / 100; }

        /// <summary>
        /// Adds values of <paramref name="sStats"/> properties to corresponding values of the <see cref="SStats"/>
        /// </summary>
        /// <param name="sStats"></param>
        public void Add(SStats sStats)
        {
            ATKPercent += sStats.ATKPercent;
            ATK += sStats.ATK;
            SPD += sStats.SPD;
            Crit += sStats.Crit;
            CritDmg += sStats.CritDmg;
            HPPercent += sStats.HPPercent;
            HP += sStats.HP;
            DEFPercent += sStats.DEFPercent;
            DEF += sStats.DEF;
            EFF += sStats.EFF;
            RES += sStats.RES;
        }

        /// <summary>
        /// Subtracts values of <paramref name="sStats"/> properties from corresponding values of the <see cref="SStats"/>
        /// </summary>
        /// <param name="sStats"></param>
        public void Subtract(SStats sStats)
        {
            ATKPercent -= sStats.ATKPercent;
            ATK -= sStats.ATK;
            SPD -= sStats.SPD;
            Crit -= sStats.Crit;
            CritDmg -= sStats.CritDmg;
            HPPercent -= sStats.HPPercent;
            HP -= sStats.HP;
            DEFPercent -= sStats.DEFPercent;
            DEF -= sStats.DEF;
            EFF -= sStats.EFF;
            RES -= sStats.RES;
        }

        /// <summary>
        /// Copies value of <paramref name="stat"/> to corresponding value of the <see cref="SStats"/>
        /// </summary>
        /// <param name="stat"></param>
        public void SetStat(Stat stat)
        {
            switch (stat.Name)
            {
                case Stats.ATKPercent:
                    ATKPercent = stat.Value;
                    break;
                case Stats.ATK:
                    ATK = stat.Value;
                    break;
                case Stats.SPD:
                    SPD = stat.Value;
                    break;
                case Stats.Crit:
                    Crit = stat.Value;
                    break;
                case Stats.CritDmg:
                    CritDmg = stat.Value;
                    break;
                case Stats.HPPercent:
                    HPPercent = stat.Value;
                    break;
                case Stats.HP:
                    HP = stat.Value;
                    break;
                case Stats.DEFPercent:
                    DEFPercent = stat.Value;
                    break;
                case Stats.DEF:
                    DEF = stat.Value;
                    break;
                case Stats.EFF:
                    EFF = stat.Value;
                    break;
                case Stats.RES:
                    RES = stat.Value;
                    break;
            }
        }

        /// <summary>
        /// Copies values of <paramref name="stats"/> properties to corresponding values of the <see cref="SStats"/>
        /// </summary>
        /// <param name="stats"></param>
        public void SetStats(Stat[] stats)
        {
            foreach (var stat in stats)
            {
                SetStat(stat);
            }
        }
    }
}
