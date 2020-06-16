using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E7_Gear_Optimizer
{
    public class Item
    {
        public string ID { get; }
        private ItemType type;
        private Set set;
        private Grade grade;
        private int iLvl;
        private int enhance;
        private Stat main;
        private Stat[] subStats;
        public SStats AllStats { get; set; } = new SStats();
        private float wss;
        public bool Locked { get; set; }
        public Hero Equipped { get; set; }

        public Item(string id, ItemType type, Set set, Grade grade, int iLvl, int enhance, Stat main, Stat[] subStats, Hero equipped, bool locked)
        {
            ID = id;
            this.type = type;
            this.set = set;
            this.grade = grade;
            this.iLvl = iLvl;
            this.enhance = enhance;
            this.main = main;
            this.subStats = subStats;
            Equipped = equipped;
            Locked = locked;

            AllStats.SetStat(main);
            AllStats.SetStats(subStats);

            calcWSS();
        }

        public Item() { }


        public ItemType Type { get => type; set => type = value; }
        public Set Set { get => set; set => set = value; }
        public Grade Grade { get => grade; set => grade = value; }
        public int ILvl
        {
            get => iLvl;
            set
            {
                if (value > 0)
                {
                    iLvl = value;
                }
            }
        }
        public int Enhance
        {
            get => enhance;
            set
            {
                if (value > -1 && value < 16)
                {
                    enhance = value;
                }
            }
        }
        public Stat Main
        {
            get => main;
            set
            {
                main = value;
                AllStats.SetStat(main);
            }
        }
        public Stat[] SubStats
        {
            get => subStats;
            set
            {
                subStats = value;
                AllStats.SetStats(subStats);
            }
        }

        public float WSS { get => wss; }

        private const float wssMultiplier = 1f / 72f;

        public void calcWSS()
        {
            wss = 0f;
            foreach (Stat s in subStats)
            {
                switch (s.Name)
                {
                    case Stats.ATKPercent:
                    case Stats.DEFPercent:
                    case Stats.HPPercent:
                    case Stats.EFF:
                    case Stats.RES:
                        wss += 100f * s.Value;
                        break;
                    case Stats.Crit:
                        wss += 100f * s.Value * 8f / 5f;
                        break;
                    case Stats.CritDmg:
                        wss += 100f * s.Value * 8f / 7f;
                        break;
                    case Stats.SPD:
                        wss += s.Value * 8f / 4f;
                        break;
                    case Stats.ATK:
                        wss += 100f * s.Value / 900f;
                        break;
                    case Stats.HP:
                        wss += 100f * s.Value / 5000f;
                        break;
                    case Stats.DEF:
                        wss += 100f * s.Value / 500f;
                        break;
                    default:
                        break;
                }
            }
            wss *= wssMultiplier;
        }
    }
}
