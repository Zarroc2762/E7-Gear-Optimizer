using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E7_Gear_Optimizer
{
    public struct Stat
    {
        private Stats name;
        private decimal value;

        public Stat(Stats name, decimal value)
        {
            this.name = name;
            this.value = name != Stats.ATK && name != Stats.DEF && name != Stats.HP && name != Stats.SPD && value >= 1 ? value / 100 : value;
        }

        public Stats Name { get => name; set => name = value; }
        public decimal Value
        {
            get => value;
            set
            {
                if (value > 0)
                {
                    this.value = value;
                }
            }
        }
    }
}
