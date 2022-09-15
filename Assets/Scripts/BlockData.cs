using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class BlockData
    {
        public string Name { get; set; }
        public float Z { get; set; }
        public bool IsWooded { get; set; }

        public override string ToString()
        {
            return $"{Name},{Z},{(IsWooded ? 1 : 0)}";
        }
    }
}
